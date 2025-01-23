using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

namespace EprPrnIntegration.Api.Functions;

public class EmailNpwdReconciliationFunction(
    IEmailService emailService,
    IAppInsightsService appInsightsService,
    IOptions<FeatureManagementConfiguration> featureConfig,
    ILogger<EmailNpwdReconciliationFunction> logger,
    IUtilities utilities,
    IPrnService prnService)
{
    [Function("EmailNpwdReconciliation")]    
    public async Task Run([TimerTrigger("%EmailNpwdReconciliationTrigger%")] TimerInfo myTimer)
    {
        var isOn = featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            logger.LogInformation("EmailNpwdReconciliation function is disabled by feature flag");
            return;
        }

        var issuedPrnEmailTask = EmailNpwdIssuedPrnsReconciliationAsync();
        var updatedPrnEmailTask = EmailUpdatedPrnReconciliationAsync();
        var updatedOrganisationsEmailTask = EmailNpwdUpdatedOrganisationsAsync();

        // Execute tasks concurrently, ensuring one task can continue even if the other fails
        await Task.WhenAll(issuedPrnEmailTask, updatedPrnEmailTask, updatedOrganisationsEmailTask);
    }

    public async Task EmailUpdatedPrnReconciliationAsync()
    {
        logger.LogInformation("EmailUpdatedPrnReconciliationAsync function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        try
        {
            var updatedPrns = await prnService.GetReconsolidatedUpdatedPrns();
            var csvData = new Dictionary<string, List<string>>
            {
                { CustomEventFields.PrnNumber, updatedPrns.Select(x => x.PrnNumber).ToList() },
                { CustomEventFields.IncomingStatus, updatedPrns.Select(x => x.StatusName).ToList() },
                { CustomEventFields.Date, updatedPrns.Select(x => x.UpdatedOn).ToList() },
                { CustomEventFields.OrganisationName, updatedPrns.Select(x => x.OrganisationName.CleanCsvString()).ToList() },
            };

            var csvContent = utilities.CreateCsvContent(csvData);

            emailService.SendUpdatedPrnsReconciliationEmailToNpwd(DateTime.UtcNow, csvContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed running EmailUpdatedPrnReconciliationAsync");
        }
    }

    public async Task EmailNpwdIssuedPrnsReconciliationAsync()
    {
        logger.LogInformation("EmailNpwdIssuedPrnsReconciliationAsync function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        try
        {
            var prns = await appInsightsService.GetIssuedPrnCustomEventLogsLast24hrsAsync();
            var csvData = new Dictionary<string, List<string>>
            {
                { CustomEventFields.PrnNumber, prns.Select(x => x.PrnNumber).ToList() },
                { CustomEventFields.IncomingStatus, prns.Select(x => x.PrnStatus).ToList() },
                { CustomEventFields.Date, prns.Select(x => x.UploadedDate).ToList() },
                { CustomEventFields.OrganisationName, prns.Select(x => x.OrganisationName.CleanCsvString()).ToList() },
            };

            var csvContent = utilities.CreateCsvContent(csvData);

            emailService.SendIssuedPrnsReconciliationEmailToNpwd(DateTime.UtcNow, prns.Count, csvContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed running EmailNpwdIssuedPrnsReconciliationAsync");
        }
    }

    public async Task EmailNpwdUpdatedOrganisationsAsync()
    {
        logger.LogInformation("{FunctionName} function executed at: {ExecutionDateTime}", nameof(EmailNpwdUpdatedOrganisationsAsync), DateTime.UtcNow);

        try
        {
            var updatedOrgs = await appInsightsService.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync();
            
            var csvData = new Dictionary<string, List<string>>
            {
                { CustomEventFields.OrganisationId, updatedOrgs.Select(x => x.Id ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationName, updatedOrgs.Select(x => x.Name ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationAddress, updatedOrgs.Select(x => x.Address ?? string.Empty).ToList() },
                { CustomEventFields.Date, updatedOrgs.Select(x => x.Date ?? string.Empty).ToList() },
            };

            var csvContent = utilities.CreateCsvContent(csvData);

            emailService.SendUpdatedOrganisationsReconciliationEmailToNpwd(DateTime.UtcNow, csvContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed running {FunctionName}", nameof(EmailNpwdUpdatedOrganisationsAsync));
        }
    }
}