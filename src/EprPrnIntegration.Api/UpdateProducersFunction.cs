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

namespace EprPrnIntegration.Api;

public class UpdateProducersFunction(
    IOrganisationService organisationService,
    INpwdClient npwdClient,
    ILogger<UpdateProducersFunction> logger,
    IConfiguration configuration,
    IUtilities utilities,
    IOptions<FeatureManagementConfiguration> featureConfig,
    IEmailService emailService)
{
    [Function("UpdateProducersList")]
    //public async Task Run([TimerTrigger("%UpdateProducersTrigger%")] TimerInfo myTimer)
    public async Task Run([HttpTrigger(AuthorizationLevel.Function, "get")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        var isOn = featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            logger.LogInformation("UpdateProducersList function is disabled by feature flag");
            return;
        }

        logger.LogInformation("UpdateProducersList function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        var deltaRun = await utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers);

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;

        logger.LogInformation("Fetching producers from {FromDate} to {ToDate}.", fromDate, toDate);

        var updatedEprProducers = await FetchUpdatedProducers(fromDate, toDate);
        if (updatedEprProducers == null || !updatedEprProducers.Any())
        {
            logger.LogWarning("No updated producers retrieved for time period {FromDate} to {ToDate}.", fromDate, toDate);
            await utilities.SetDeltaSyncExecution(deltaRun, toDate);
            return;
        }

        var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers, configuration);

        try
        {
            var pEprApiResponse = await npwdClient.Patch(npwdUpdatedProducers, NpwdApiPath.UpdateProducers);

            if (pEprApiResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Producers list successfully updated in NPWD for time period {FromDate} to {ToDate}.", fromDate,
                    toDate);

                await utilities.SetDeltaSyncExecution(deltaRun, toDate);
            }
            else
            {
                var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
                logger.LogError(
                    "Failed to update producer lists. error code {StatusCode} and raw response body: {ResponseBody}",
                    pEprApiResponse.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"An error was encountered on attempting to call NPWD API {NpwdApiPath.UpdateProducers}");
            emailService!.SendEmailToNpwd(ex.Message);
        }
    }

    private async Task<List<UpdatedProducersResponseModel>> FetchUpdatedProducers(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await organisationService.GetUpdatedProducers(fromDate, toDate, new CancellationToken());
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve data from accounts backend for time period {FromDate} to {ToDate}.", fromDate, toDate);
            return null;
        }
    }
}