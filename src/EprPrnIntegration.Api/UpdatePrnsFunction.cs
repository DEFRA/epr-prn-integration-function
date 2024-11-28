using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api;

public class UpdatePrnsFunction(IPrnService prnService, INpwdClient npwdClient,
    ILogger<UpdatePrnsFunction> logger, IConfiguration configuration)
{
    [Function("UpdatePrnsList")]
    public async Task Run(
        [TimerTrigger("%UpdatePrnsTrigger%")] TimerInfo myTimer)
    {
        logger.LogInformation($"UpdatePrnsList function executed at: {DateTime.UtcNow}");

        // Read the start hour (e.g., 18 for 6 PM) from configuration
        var startHourConfig = configuration["UpdatePrnsStartHour"];
        if (!int.TryParse(startHourConfig, out var startHour) || startHour < 0 || startHour > 23)
        {
            logger.LogError(
                $"Invalid StartHour configuration value: {startHourConfig}. Using default value of 18(6pm).");
            startHour = 18; // Default to 6 PM if configuration is invalid
        }

        // Calculate fromDate and toDate
        var toDate = DateTime.Today.AddHours(startHour); // Configurable hour today
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
        var npwdUpdatedPrns = (List<UpdatedPrnsResponseModel>)updatedEprPrns;

        var pEprApiResponse = await npwdClient.Patch(npwdUpdatedPrns, NpwdApiPath.UpdatePrns);

        if (pEprApiResponse.IsSuccessStatusCode)
        {
            logger.LogInformation(
                $"Prns list successfully updated in NPWD for time period {fromDate} to {toDate}.");
        }
        else
        {
            logger.LogError($"Failed to update Prns list in NPWD. Status Code: {pEprApiResponse.StatusCode}");
        }
    }
}