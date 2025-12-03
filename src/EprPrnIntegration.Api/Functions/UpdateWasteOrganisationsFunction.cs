using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

[ExcludeFromCodeCoverage]
public class UpdateWasteOrganisationsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<UpdateWasteOrganisationsFunction> logger,
    IWasteOrganisationsService wasteOrganisationsService
    )
{
    [Function("UpdateWasteOrganisations")]
    public async Task Run([TimerTrigger("%UpdateWasteOrganisationsTrigger%")] TimerInfo myTimer)
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
        logger.LogInformation("UpdateWasteOrganisationsList function executed at: {ExecutionDateTime}", lastUpdate);

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", DateTime.UtcNow);

        await wasteOrganisationsService.GetOrganisation(Guid.NewGuid().ToString());
    }
}