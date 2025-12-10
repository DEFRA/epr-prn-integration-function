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
    public async Task Run([TimerTrigger("%UpdateWasteOrganisations:Trigger%")] TimerInfo myTimer)
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;
        logger.LogInformation("UpdateWasteOrganisationsList resuming with last update time: {ExecutionDateTime}", lastUpdate);
        
        var now = DateTime.UtcNow;
        
        var producers = await commonDataService.GetUpdatedProducersV2(lastUpdate, now, CancellationToken.None);

        if (!producers.Any())
        {
            logger.LogInformation("No freshly updated producers were found; terminating.");
            return;
        }

        await UpdateProducers(producers);

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", now);
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
            // Allow the function to terminate and resume on the next schedule with the original time window.
            logger.LogError(ex, "Service unavailable ({StatusCode}) when updating organisation {OrganisationId}, rethrowing", ex.StatusCode, producer.PEPRID);
            throw;
        }
        catch (Exception ex)
        {
            // We want to swallow non-transient errors since they'll never be recoverable; all we can do is log errors
            // to allow investigation.
            logger.LogError(ex, "Failed to update organisation {OrganisationId}, continuing with next producer", producer.PEPRID);
        }
    }

    // 5xx, 408 or 429.
    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is >= HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
    }
}