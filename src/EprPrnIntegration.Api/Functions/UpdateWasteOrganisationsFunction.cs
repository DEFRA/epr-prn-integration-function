using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

public class UpdateWasteOrganisationsFunction(
    ICommonDataService commonDataService,
    INpwdClient npwdClient,
    ILogger<UpdateWasteOrganisationsFunction> logger,
    IConfiguration configuration,
    IUtilities utilities,
    IOptions<FeatureManagementConfiguration> featureConfig,
    IEmailService emailService)
{
    [Function("UpdateWasteOrganisations")]
    public async Task<bool> Run([TimerTrigger("%UpdateWasteOrganisationsTrigger%")] TimerInfo myTimer)
    {
        // Empty implementation
        logger.LogInformation("UpdateWasteOrganisationsList function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        return true;
    }
}
