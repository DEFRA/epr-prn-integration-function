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

        // Execute tasks concurrently, ensuring one task can continue even if the other fails
        await Task.WhenAll(issuedPrnEmailTask, updatedPrnEmailTask);
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
                { CustomEventFields.IncomingStatus, reconciledPrns.Select(x => x.StatusName).ToList() },
                { CustomEventFields.Date, reconciledPrns.Select(x => x.UpdatedOn).ToList() },
                { CustomEventFields.OrganisationName, reconciledPrns.Select(x => x.OrganisationName.CleanCsvString()).ToList() },
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
}