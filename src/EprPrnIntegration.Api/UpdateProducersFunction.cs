using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api;

public class UpdateProducersFunction(
    IOrganisationService organisationService,
    INpwdClient npwdClient,
    ILogger<UpdateProducersFunction> logger,
    IConfiguration configuration,
    IServiceBusProvider serviceBusProvider)
{
    [Function("UpdateProducersList")]
    public async Task Run([TimerTrigger("%UpdateProducersTrigger%")] TimerInfo myTimer)
    {
        logger.LogInformation($"UpdateProducersList function executed at: {DateTime.UtcNow}");

        var deltaRun = await DeltaSyncExecution();

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;
         
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

        deltaRun.LastSyncDateTime = DateTime.UtcNow;
        await serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaRun);
    }

    private async Task<DeltaSyncExecution> DeltaSyncExecution()
    {
        var deltaMessage =
            await serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);
        return deltaMessage ?? new DeltaSyncExecution
        {
            LastSyncDateTime = DateTime.Parse(configuration["DefaultLastRunDate"]),
            SyncType = NpwdDeltaSyncType.UpdatedProducers
        };
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
                $"Failed to retrieve data from accounts backend for time period {fromDate} to {toDate}.");
            return null;
        }
    }

    private async Task HandleApiResponse(HttpResponseMessage pEprApiResponse, DateTime fromDate, DateTime toDate)
    {
        if (pEprApiResponse.IsSuccessStatusCode)
        {
            logger.LogInformation(
                $"Producers list successfully updated in NPWD for time period {fromDate} to {toDate}.");
            return;
        }

        var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
        logger.LogError($"Failed to parse error response body. Raw Response Body: {responseBody}");
    }
}