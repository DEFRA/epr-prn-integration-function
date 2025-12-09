using System.Net;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class UpdateWasteOrganisationsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<UpdateWasteOrganisationsFunction> logger,
    ICommonDataService commonDataService,
    IWasteOrganisationsService wasteOrganisationsService)
{
    [Function("UpdateWasteOrganisations")]
    public async Task Run([TimerTrigger("%UpdateWasteOrganisationsTrigger%")] TimerInfo myTimer)
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;
        logger.LogInformation("UpdateWasteOrganisationsList resuming with last update time: {ExecutionDateTime}", lastUpdate);
        
        var producers = await GetProducersToUpdate(lastUpdate);

        if (!producers.Any())
        {
            logger.LogInformation("No freshly updated producers were found; terminating.");
            return;
        }

        await UpdateProducers(producers);

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", DateTime.UtcNow);
    }

    private async Task UpdateProducers(List<UpdatedProducersResponseV2> producers)
    {
        await Parallel.ForEachAsync(producers, new ParallelOptions
        {
            MaxDegreeOfParallelism = 20
        }, async (producer, _) => await UpdateProducer(producer));
    }

    private async Task UpdateProducer(UpdatedProducersResponseV2 producer)
    {
        try
        {
            var request = WasteOrganisationsApiUpdateRequestMapper.Map(producer);
            await wasteOrganisationsService.UpdateOrganisation(producer.PEPRID!, request);
        }
        catch (HttpRequestException ex) when (IsTransient(ex))
        {
            logger.LogError(ex, "Service unavailable ({StatusCode}) when updating organisation {OrganisationId}, rethrowing", ex.StatusCode, producer.PEPRID);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update organisation {OrganisationId}, continuing with next producer", producer.PEPRID);
        }
    }

    private async Task<List<UpdatedProducersResponseV2>> GetProducersToUpdate(DateTime lastUpdate)
    {
        try
        {
            var producers =
                await commonDataService.GetUpdatedProducersV2(lastUpdate, DateTime.UtcNow, CancellationToken.None);
            return producers;
        }
        catch (Exception e)
        {
            logger.LogError("Failed to fetch producers: {Exception}", e);
            return [];
        }
    }
    
   // 5xx, 408 or 429.
    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is >= HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
    }
}