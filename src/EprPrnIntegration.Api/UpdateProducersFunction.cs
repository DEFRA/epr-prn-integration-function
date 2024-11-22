using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api;

public class UpdateProducersFunction(IOrganisationService organisationService, INpwdClient npwdClient, 
    ILogger<UpdateProducersFunction> logger, IConfiguration configuration)
{
    [Function("UpdateProducersList")]
    public async Task Run(
        [TimerTrigger("%UpdateProducersTrigger%")] TimerInfo myTimer)
    {
        logger.LogInformation($"UpdateProducersList function executed at: {DateTime.UtcNow}");

        // Read the start hour (e.g., 18 for 6 PM) from configuration
        var startHourConfig = configuration["UpdateProducersStartHour"];
        if (!int.TryParse(startHourConfig, out var startHour) || startHour < 0 || startHour > 23)
        {
            logger.LogError(
                $"Invalid StartHour configuration value: {startHourConfig}. Using default value of 18(6pm).");
            startHour = 18; // Default to 6 PM if configuration is invalid
        }

        // Calculate fromDate and toDate
        var toDate = DateTime.Today.AddHours(startHour); // Configurable hour today
        var fromDate = toDate.AddDays(-1); // Same hour yesterday

        logger.LogInformation($"Fetching producers from {fromDate} to {toDate}.");

        // Retrieve data from the accounts backend
        List<UpdatedProducersResponseModel> updatedEprProducers;
        try
        {
            updatedEprProducers =
                await organisationService.GetUpdatedProducers(fromDate, toDate, new CancellationToken());
            if (updatedEprProducers == null || !updatedEprProducers.Any())
            {
                logger.LogWarning(
                    $"No updated producers is retrieved from account database form time period {fromDate} to {toDate}.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                $"Failed to retrieve data from accounts backend. form time period {fromDate} to {toDate}.");
            return;
        }

        // Send data to NPWD via pEPR API
        var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers);

        var pEprApiResponse = await npwdClient.Patch(npwdUpdatedProducers, NpwdApiPath.UpdateProducers);

        if (pEprApiResponse.IsSuccessStatusCode)
        {
            logger.LogInformation(
                $"Producers list successfully updated in NPWD for time period {fromDate} to {toDate}.");
        }
        else
        {
            logger.LogError($"Failed to update producers list in NPWD. Status Code: {pEprApiResponse.StatusCode}");
        }
    }
}