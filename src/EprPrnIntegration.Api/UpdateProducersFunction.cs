using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
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
    IServiceBusProvider serviceBusProvider,
    IOptions<FeatureManagementConfiguration> featureConfig)
{
    [Function("UpdateProducersList")]
    public async Task Run([TimerTrigger("%UpdateProducersTrigger%")] TimerInfo myTimer)
    {
        var isOn = featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            logger.LogInformation("UpdateProducersList function is disabled by feature flag");
            return;
        }

        logger.LogInformation("UpdateProducersList function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        var deltaRun = await DeltaSyncExecution();

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;
         
        logger.LogInformation("Fetching producers from {FromDate} to {ToDate}.", fromDate, toDate);

        var updatedEprProducers = await FetchUpdatedProducers(fromDate, toDate);
        if (updatedEprProducers == null || !updatedEprProducers.Any())
        {
            logger.LogWarning("No updated producers retrieved for time period {FromDate} to {ToDate}.", fromDate, toDate);
            return;
        }

        var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers, configuration);
        var pEprApiResponse = await npwdClient.Patch(npwdUpdatedProducers, NpwdApiPath.UpdateProducers);

        if (pEprApiResponse.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "Producers list successfully updated in NPWD for time period {FromDate} to {ToDate}.", fromDate, toDate);
            
            // set lastSyncDateTime to toDate as we may receive update during execution 
            deltaRun.LastSyncDateTime = toDate;
            await serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaRun);
        }
        else
        {
            var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to update producer lists. error code {StatusCode} and raw response body: {ResponseBody}",
                pEprApiResponse.StatusCode, responseBody);
        }
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
                "Failed to retrieve data from accounts backend for time period {FromDate} to {ToDate}.", fromDate, toDate);
            return null;
        }
    }
}