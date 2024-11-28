using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api;

public class UpdateProducersFunction(IOrganisationService organisationService, INpwdClient npwdClient, 
    ILogger<UpdateProducersFunction> logger, IConfiguration configuration)
{
    [Function("UpdateProducersList")]
    public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        logger.LogInformation($"UpdateProducersList function executed at: {DateTime.UtcNow}");

        int startHour = GetStartHour(configuration["UpdateProducersStartHour"]);

        var toDate = DateTime.Today.AddHours(startHour);
        var fromDate = toDate.AddDays(-1);

        logger.LogInformation($"Fetching producers from {fromDate} to {toDate}.");

        var updatedEprProducers = await FetchUpdatedProducers(fromDate, toDate);
        if (updatedEprProducers == null || !updatedEprProducers.Any())
        {
            logger.LogWarning($"No updated producers retrieved for time period {fromDate} to {toDate}.");
            return;
        }

        var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers, configuration);
        var pEprApiResponse = await npwdClient.Patch(npwdUpdatedProducers, NpwdApiPath.UpdateProducers);

        await HandleApiResponse(pEprApiResponse, fromDate, toDate);
    }

    private int GetStartHour(string startHourConfig)
    {
        if (!int.TryParse(startHourConfig, out var startHour) || startHour < 0 || startHour > 23)
        {
            logger.LogError($"Invalid StartHour configuration value: {startHourConfig}. Using default value of 18 (6 PM).");
            return 18; // Default to 6 PM if configuration is invalid
        }
        return startHour;
    }

    private async Task<List<UpdatedProducersResponseModel>> FetchUpdatedProducers(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await organisationService.GetUpdatedProducers(fromDate, toDate, new CancellationToken());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to retrieve data from accounts backend for time period {fromDate} to {toDate}.");
            return null;
        }
    }

    private async Task HandleApiResponse(HttpResponseMessage pEprApiResponse, DateTime fromDate, DateTime toDate)
    {
        if (pEprApiResponse.IsSuccessStatusCode)
        {
            logger.LogInformation($"Producers list successfully updated in NPWD for time period {fromDate} to {toDate}.");
            return;
        }

        var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
        logger.LogError($"Failed to parse error response body. Raw Response Body: {responseBody}");
    }
}