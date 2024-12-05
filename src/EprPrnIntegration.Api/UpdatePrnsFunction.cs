using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api;

public class UpdatePrnsFunction(IPrnService prnService, INpwdClient npwdClient,
    ILogger<UpdatePrnsFunction> logger, IConfiguration configuration, IOptions<FeatureManagementConfiguration> featureConfig)
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

        // Read the start hour (e.g., 18 for 6 PM) from configuration
        var startHourConfig = configuration["UpdatePrnsStartHour"];
        if (!int.TryParse(startHourConfig, out var startHourParsed) || startHourParsed < 0 || startHourParsed > 23)
        {
            logger.LogError($"Invalid StartHour configuration value: {startHourConfig}. Using default value of 18(6pm).");
            startHourParsed = 18; // Default to 6 PM if configuration is invalid
        }

        // Calculate fromDate and toDate
        var toDate = DateTime.Today.AddHours(startHourParsed); // Configurable hour today
        var fromDate = toDate.AddDays(-1); // Same hour yesterday

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
        var npwdUpdatedPrns = updatedEprPrns;

        var pEprApiResponse = await npwdClient.Patch(npwdUpdatedPrns, NpwdApiPath.UpdatePrns);

        if (pEprApiResponse.IsSuccessStatusCode)
        {
            logger.LogInformation(
                $"Prns list successfully updated in NPWD for time period {fromDate} to {toDate}.");
        }
        else
        {
            var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to update producer lists. error code {StatusCode} and raw response body: {ResponseBody}",
                pEprApiResponse.StatusCode, responseBody);
            logger.LogError($"Failed to update Prns list in NPWD. Status Code: {pEprApiResponse.StatusCode}");
        }
    }
}