using System.Net;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices;
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
    IOptions<UpdateWasteOrganisationsConfiguration> config
)
{
    [Function(FunctionName.UpdateWasteOrganisations)]
    public async Task Run(
        [TimerTrigger($"%{FunctionName.UpdateWasteOrganisations}:Trigger%")] TimerInfo myTimer
    )
    {
        var lastUpdate = await GetLastUpdate();
        logger.LogInformation(
            $"%{FunctionName.UpdateWasteOrganisations} resuming with last update time: {{ExecutionDateTime}}",
            lastUpdate
        );

        var utcNow = DateTime.UtcNow;

        var producers = await commonDataService.GetUpdatedProducersV2(
            lastUpdate,
            utcNow,
            CancellationToken.None
        );

        if (!producers.Any())
        {
            logger.LogInformation("No freshly updated producers were found; terminating.");
            return;
        }

        await UpdateProducers(producers);

        await lastUpdateService.SetLastUpdate(FunctionName.UpdateWasteOrganisations, utcNow);
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate(
            FunctionName.UpdateWasteOrganisations
        );
        if (!lastUpdate.HasValue)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(
                    config.Value.DefaultStartDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                DateTimeKind.Utc
            );
        }
        return lastUpdate!.Value;
    }

    private async Task UpdateProducers(List<UpdatedProducersResponseV2> producers)
    {
        logger.LogInformation("Found {ProducerCount} updated producers ", producers.Count);
        foreach (var producer in producers)
            await UpdateProducer(producer);
    }

    private async Task UpdateProducer(UpdatedProducersResponseV2 producer)
    {
        await HttpHelper.HandleTransientErrors(
            async () =>
            {
                var request = WasteOrganisationsApiUpdateRequestMapper.Map(producer);
                return await wasteOrganisationsService.UpdateOrganisation(
                    producer.PEPRID!,
                    request
                );
            },
            logger,
            $"Saving Organisation {producer.PEPRID}"
        );
    }
}
