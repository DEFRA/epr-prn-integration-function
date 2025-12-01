using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class UpdateWasteOrganisationsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<UpdateWasteOrganisationsFunction> logger)
{
    [Function("UpdateWasteOrganisations")]
    public async Task Run([TimerTrigger("%UpdateWasteOrganisationsTrigger%")] TimerInfo myTimer)
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
        logger.LogInformation("UpdateWasteOrganisationsList function executed at: {ExecutionDateTime}", lastUpdate);

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", DateTime.UtcNow);
    }
}
