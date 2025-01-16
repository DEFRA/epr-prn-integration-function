using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace EprPrnIntegration.Api.Functions;

public class EmailNpwdReconciliationFunction(
    IEmailService _emailService,
    IAppInsightsService _appInsightsService,
    IOptions<FeatureManagementConfiguration> _featureConfig,
    ILogger<EmailNpwdReconciliationFunction> _logger
)
{
    [Function("EmailNpwdReconciliation")]
    public async Task Run([TimerTrigger("%EmailNpwdReconciliationTrigger%")] TimerInfo myTimer)
    {
        var isOn = _featureConfig.Value.RunReconciliation ?? false;
        if (!isOn)
        {
            _logger.LogInformation("EmailNpwdReconciliation function(s) disabled by feature flag");
            return;
        }

        await EmailNpwdIssuedPrnsReconciliationAsync();

    }

    public async Task EmailNpwdIssuedPrnsReconciliationAsync()
    {
        _logger.LogInformation("EmailNpwdIssuedPrnsReconciliationAsync function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        string csv = string.Empty;
        List<ReconcileIssuedPrn> prns = new List<ReconcileIssuedPrn>();

        try
        {
            // retrieve custom events from Application Insights
            prns = await _appInsightsService.GetIssuedPrnCustomEventLogsLast24hrsAsync();

            // generate csv
            csv = TransformPrnsToCsv(prns);

            // send reconciliation email with link to csv file
            _emailService.SendIssuedPrnsReconciliationEmailToNpwd(DateTime.UtcNow, prns.Count, csv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed running EmailNpwdIssuedPrnsReconciliationAsync");
        }
    }

    public static string TransformPrnsToCsv(List<ReconcileIssuedPrn> prns)
    {
        StringBuilder sb = new StringBuilder();

        // header
        sb.Append(CustomEventFields.PrnNumber).Append(',');
        sb.Append(CustomEventFields.IncomingStatus).Append(',');
        sb.Append(CustomEventFields.Date).Append(',');
        sb.AppendLine(CustomEventFields.OrganisationName);

        if (prns == null || prns.Count == 0)
            return sb.ToString();

        foreach (var prn in prns)
        {
            sb.Append(prn.PrnNumber).Append(',');
            sb.Append(prn.PrnStatus).Append(',');
            sb.Append(prn.UploadedDate).Append(',');
            sb.Append(prn.OrganisationName.CleanCsvString()).AppendLine();
        }

        return sb.ToString();

    }

}