using System.Net;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

public class UpdateWasteOrganisationsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<UpdateWasteOrganisationsFunction> logger,
    ICommonDataService commonDataService,
    IWasteOrganisationsService wasteOrganisationsService,
    IOptions<UpdateWasteOrganisationsConfiguration> config)
{
    [Function("UpdateWasteOrganisations")]
    public async Task Run([TimerTrigger("%UpdateWasteOrganisations:Trigger%")] TimerInfo myTimer)
    {
        var lastUpdate = await GetLastUpdate();
        logger.LogInformation("UpdateWasteOrganisationsList resuming with last update time: {ExecutionDateTime}", lastUpdate);

        var utcNow = DateTime.UtcNow;
        
        var producers = await commonDataService.GetUpdatedProducersV2(lastUpdate, utcNow, CancellationToken.None);

        if (!producers.Any())
        {
            logger.LogInformation("No freshly updated producers were found; terminating.");
            return;
        }

        await UpdateProducers(producers);

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", utcNow);
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
        if (!lastUpdate.HasValue)
        {
           return DateTime.SpecifyKind(
               DateTime.ParseExact(config.Value.DefaultStartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
               DateTimeKind.Utc
           );
        }
        return lastUpdate!.Value;
    }

    private async Task UpdateProducers(List<UpdatedProducersResponseV2> producers)
    {
        logger.LogInformation("Found {ProducerCount} updated producers ", producers.Count);
        foreach (var producer in producers)
        {
            await UpdateProducer(producer);
        }
    }

    private async Task UpdateProducer(UpdatedProducersResponseV2 producer)
    {
        try
        {
            var request = WasteOrganisationsApiUpdateRequestMapper.Map(producer, logger);
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