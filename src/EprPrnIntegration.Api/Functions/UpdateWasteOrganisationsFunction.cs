using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class UpdateWasteOrganisationsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<UpdateWasteOrganisationsFunction> logger)
{
    [Function("UpdateWasteOrganisations")]
    public async Task<bool> Run([TimerTrigger("%UpdateWasteOrganisationsTrigger%")] TimerInfo myTimer)
    {
        logger.LogInformation("UpdateWasteOrganisationsList function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        await lastUpdateService.SetLastUpdate("UpdateWasteOrganisations", DateTime.UtcNow);

        return true;
    }
}
