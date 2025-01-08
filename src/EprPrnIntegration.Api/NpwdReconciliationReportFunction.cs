using Azure.Identity;
using Azure.Monitor.Query;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace EprPrnIntegration.Api;

public class EmailReconcilitionReportToNpwdFunction(
    IEmailService _emailService,
    IOptions<FeatureManagementConfiguration> _featureConfig,
    ILogger<EmailReconcilitionReportToNpwdFunction> _logger
)
{
    [Function("NpwdReconciliationReportFunction")]
    public async Task Run([TimerTrigger("%NpwdReconciliationReport%")] TimerInfo myTimer)
    {
        var isOn = _featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            _logger.LogInformation("NpwdReconciliationReport function is disabled by feature flag");
            return;
        }

        _logger.LogInformation("NpwdReconciliationReport function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        DateTime reportDate = DateTime.UtcNow;
        var customLogs = await GetCustomLogsForPrnsReceivedPrevious24hrsAsync();
        if (customLogs != null)
        {
            int rowCount = 0;
            StringBuilder sb = new StringBuilder();
            foreach (Azure.Monitor.Query.Models.LogsTable? table in customLogs.Value.AllTables)
            {
                rowCount += table.Rows.Count;
                foreach (var row in table.Rows)
                {
                    sb.Append(string.Join(", ", row));
                }
            }
            _emailService.SendReconciliationEmailToNpwd(reportDate, rowCount, sb.ToString());
        }
    }

    public async Task<Azure.Response<Azure.Monitor.Query.Models.LogsQueryResult>> GetCustomLogsForPrnsReceivedPrevious24hrsAsync()
    {
        LogsQueryClient client = new LogsQueryClient(new DefaultAzureCredential());
        string resourceId = @"/subscriptions/b680e2ba-654e-4e1b-93d7-c8cb2a01409e/resourceGroups/Eviden/providers/microsoft.insights/components/readinglogsfromappinsights";
        string logName = "PrnValidationError";
        string query = string.Concat("customEvents | where name startswith '", logName, "'");
        
        // Run the query on the Application Insights resource
        return await client.QueryResourceAsync(new Azure.Core.ResourceIdentifier(resourceId), query, new QueryTimeRange(TimeSpan.FromDays(1)));

    }

}