using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

namespace EprPrnIntegration.Api.Functions;

public class UpdatePrnsFunction(IPrnService prnService, INpwdClient npwdClient,
    ILogger<UpdatePrnsFunction> logger,
    IConfiguration configuration,
    IOptions<FeatureManagementConfiguration> featureConfig,
    IUtilities utilities, IEmailService _emailService)
{
    [Function("UpdatePrnsList")]
    public async Task Run(
    [TimerTrigger("%UpdatePrnsTrigger%")] TimerInfo myTimer)
    {
        bool isOn = featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            logger.LogInformation("UpdatePrnsList function is disabled by feature flag");
            return;
        }

        logger.LogInformation($"UpdatePrnsList function executed at: {DateTime.UtcNow}");

        var deltaRun = await utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns);

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;

        logger.LogInformation($"Fetching Prns from {fromDate} to {toDate}.");

        // Retrieve data from the common backend
        List<UpdatedPrnsResponseModel> updatedEprPrns;
        try
        {
            updatedEprPrns =
                await prnService.GetUpdatedPrns(fromDate, toDate, new CancellationToken());
            if (updatedEprPrns == null || !updatedEprPrns.Any())
            {
                logger.LogWarning(
                    $"No updated Prns are retrieved from common database form time period {fromDate} to {toDate}.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                $"Failed to retrieve data from common backend. form time period {fromDate} to {toDate}.");
            return;
        }

        // Send data to NPWD via pEPR API
        var npwdUpdatedPrns = PrnMapper.Map(updatedEprPrns, configuration);

        try
        {
            var pEprApiResponse = await npwdClient.Patch(npwdUpdatedPrns, NpwdApiPath.UpdatePrns);

            if (pEprApiResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    $"Prns list successfully updated in NPWD for time period {fromDate} to {toDate}.");

                await utilities.SetDeltaSyncExecution(deltaRun, toDate);
                // Insert sync data into common prn backend
                await prnService.InsertPeprNpwdSyncPrns(npwdUpdatedPrns.Value);
            }
            else
            {
                var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
                logger.LogError(
                    "Failed to update producer lists. error code {StatusCode} and raw response body: {ResponseBody}",
                    pEprApiResponse.StatusCode, responseBody);
                logger.LogError($"Failed to update Prns list in NPWD. Status Code: {pEprApiResponse.StatusCode}");

                if (pEprApiResponse.StatusCode >= HttpStatusCode.InternalServerError || pEprApiResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    _emailService.SendErrorEmailToNpwd($"Failed to update producer lists. error code {pEprApiResponse.StatusCode} and raw response body: {responseBody}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to patch NpwdUpdatedPrns for {npwdUpdatedPrns?.ToString()}");
        }
    }
}