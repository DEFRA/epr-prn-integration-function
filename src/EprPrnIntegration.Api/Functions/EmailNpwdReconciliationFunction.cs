using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        var isOn = featureConfig.Value.RunReconciliation ?? false;
        if (!isOn)
        {
            logger.LogInformation("EmailNpwdReconciliation function(s) disabled by feature flag");
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
            var reconciledPrns = await prnService.GetReconciledUpdatedPrns();

            var csvData = new Dictionary<string, List<string>>
            {
                { CustomEventFields.PrnNumber, reconciledPrns.Select(x => x.PrnNumber).ToList() },
                { CustomEventFields.OutgoingStatus, reconciledPrns.Select(x => x.StatusName).ToList() },
                { CustomEventFields.Date, reconciledPrns.Select(x => x.UpdatedOn).ToList() },
                { CustomEventFields.OrganisationName, reconciledPrns.Select(x => x.OrganisationName.CleanCsvString()).ToList() },
            };

            var csvContent = utilities.CreateCsvContent(csvData);

            emailService.SendUpdatedPrnsReconciliationEmailToNpwd(DateTime.UtcNow, csvContent, reconciledPrns.Count);
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

            logger.LogInformation("Count of updated organisations fetched from {AppInsightSvcFunctionName} =  {UpdatedOrgCount} at {UpdatedDateTime}", 
                nameof(appInsightsService.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync), updatedOrgs?.Count ?? 0, DateTime.UtcNow);

            var csvData = new Dictionary<string, List<string>>
            {
                { CustomEventFields.OrganisationName, updatedOrgs.Select(x => x.Name ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationId, updatedOrgs.Select(x => x.Id ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationAddress, updatedOrgs.Select(x => x.Address ?? string.Empty).ToList() },
                { CustomEventFields.Date, updatedOrgs.Select(x => x.Date ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationType, updatedOrgs.Select(x => x.OrganisationType ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationStatus, updatedOrgs.Select(x => x.Status ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationEprId, updatedOrgs.Select(x => x.PEPRId ?? string.Empty).ToList() },
                { CustomEventFields.OrganisationRegNo, updatedOrgs.Select(x => x.CompanyRegNo ?? string.Empty).ToList() }
            };

            var csvContent = utilities.CreateCsvContent(csvData);

            var dataRowsCount = updatedOrgs?.Count ?? 0;
            emailService.SendUpdatedOrganisationsReconciliationEmailToNpwd(DateTime.UtcNow, dataRowsCount, csvContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed running {FunctionName}", nameof(EmailNpwdUpdatedOrganisationsAsync));
        }
    }
}