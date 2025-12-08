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
        logger.LogInformation("UpdateWasteOrganisationsList function executed at: {ExecutionDateTime}", lastUpdate);
        
        var producers = await GetUpdatedProducers(lastUpdate);

        if (!producers.Any())
        {
            logger.LogInformation("No waste organisations were found, terminating.");
            return;
        }

        foreach (var producer in producers)
        {
            var request = WasteOrganisationsApiUpdateRequestMapper.Map(producer);
            await wasteOrganisationsService.UpdateOrganisation(producer.PEPRID!, request);
        }

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", DateTime.UtcNow);
    }

    private async Task<List<UpdatedProducersResponseV2>> GetUpdatedProducers(DateTime lastUpdate)
    {
        try
        {
            var producers =
                await commonDataService.GetUpdatedProducersV2(lastUpdate, DateTime.UtcNow, CancellationToken.None);
            return producers;
        }
        catch (Exception e)
        {
            logger.LogError("Failed to fetch updated producers: {Exception}", e);
            return [];
        }
    }
}