using Azure.Identity;
using Azure.Monitor.Query;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace EprPrnIntegration.Api.Functions;

public class NpwdReconciliationReportFunction(
    IEmailService _emailService,
    IOptions<FeatureManagementConfiguration> _featureConfig,
    ILogger<NpwdReconciliationReportFunction> _logger
)
{
    // for querying Application Insights
    private const string prn_Number = "prnNumber";
    private const string status = "status";
    private const string report_Date = "reportDate";
    private const string org_Name = "orgName";

    [Function("NpwdReconciliationReport")]
    public async Task Run([TimerTrigger("%NpwdReconciliationReportTrigger%")] TimerInfo myTimer)
    {
        var isOn = _featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            _logger.LogInformation("NpwdReconciliationReport function is disabled by feature flag");
            return;
        }

        _logger.LogInformation("NpwdReconciliationReport function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        try
        {
            int rowCount = 0;
            StringBuilder sb = new StringBuilder();

            // retrieve custom events from Application Insights
            var customLogs = await GetCustomEventLogsLast24hrsAsync();

            // generate csv
            if (customLogs != null)
            {
                foreach (Azure.Monitor.Query.Models.LogsTable? table in customLogs.Value.AllTables)
                {
                    rowCount += TransformCustomEventLogToCsv(sb, table);
                }
            }

            // send reconciliation email with link to csv file
            _emailService.SendReconciliationEmailToNpwd(DateTime.UtcNow, rowCount, sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed running NpwdReconciliationReport");
        }

    }

    public async Task<Azure.Response<Azure.Monitor.Query.Models.LogsQueryResult>> GetCustomEventLogsLast24hrsAsync()
    {
        LogsQueryClient client = new LogsQueryClient(new DefaultAzureCredential());
        string resourceId = @"/subscriptions/b680e2ba-654e-4e1b-93d7-c8cb2a01409e/resourceGroups/Eviden/providers/microsoft.insights/components/readinglogsfromappinsights";
        
        string query = @$"customEvents
                            | where name in ('{CustomEvents.IssuedPrn}')
                            | extend prn = parse_json(customDimensions)
                            | extend {prn_Number} = prn['{CustomEventFields.PrnNumber}'], 
                                {status} = prn['{CustomEventFields.IncomingStatus}'], 
                                {report_Date} = prn['{CustomEventFields.Date}'], 
                                {org_Name} = prn['{CustomEventFields.OrganisationName}']
                            | project {prn_Number}, {status}, {report_Date}, {org_Name}";

        // Run the query on the Application Insights resource
        return await client.QueryResourceAsync(new Azure.Core.ResourceIdentifier(resourceId), query, new QueryTimeRange(TimeSpan.FromDays(1)));

    }

    private static int TransformCustomEventLogToCsv(StringBuilder sb, Azure.Monitor.Query.Models.LogsTable? table)
    {
        // header
        sb.Append(CustomEventFields.PrnNumber).Append(',');
        sb.Append(CustomEventFields.IncomingStatus).Append(',');
        sb.Append(CustomEventFields.Date).Append(',');
        sb.AppendLine(CustomEventFields.OrganisationName);

        if (table == null)
            return 0;

        foreach (var row in table.Rows)
        {
            sb.Append(row[prn_Number]).Append(',');
            sb.Append(row[status]).Append(',');
            sb.Append(row[report_Date]).Append(',');

            string org = row[org_Name].ToString() ?? string.Empty;
            sb.Append(org.CleanCsvString()).AppendLine();
        }

        return table.Rows.Count;
    }

}