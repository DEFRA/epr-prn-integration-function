using Azure.Identity;
using Azure.Monitor.Query;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage(Justification = "Azure Default Credentials and LogsQueryClient are difficult to Mock")]
public class AppInsightsService : IAppInsightsService
{
    private readonly IOptions<AppInsightsConfig> _appInsightsConfig;
    public AppInsightsService(IOptions<AppInsightsConfig> appInsightsConfig)
    {
        _appInsightsConfig = appInsightsConfig;
    }

    public async Task<List<ReconcileIssuedPrn>> GetIssuedPrnCustomEventLogsLast24hrsAsync()
    {
        var prns = new List<ReconcileIssuedPrn>();

        const string prn_Number = "prnNumber";
        const string status = "status";
        const string report_Date = "reportDate";
        const string org_Name = "orgName";

        LogsQueryClient client = new(new DefaultAzureCredential());
        string resourceId = _appInsightsConfig.Value.ResourceId;

        string query = @$"customEvents
                            | where name in ('{CustomEvents.IssuedPrn}')
                            | extend prn = parse_json(customDimensions)
                            | extend {prn_Number} = prn['{CustomEventFields.PrnNumber}'], 
                                {status} = prn['{CustomEventFields.IncomingStatus}'], 
                                {report_Date} = prn['{CustomEventFields.Date}'], 
                                {org_Name} = prn['{CustomEventFields.OrganisationName}']
                            | project {prn_Number}, {status}, {report_Date}, {org_Name}";

        // run the query on the Application Insights resource
        var customLogs = await client.QueryResourceAsync(new Azure.Core.ResourceIdentifier(resourceId), query, new QueryTimeRange(TimeSpan.FromDays(1)));

        if (customLogs != null)
        {
            foreach (Azure.Monitor.Query.Models.LogsTable? table in customLogs.Value.AllTables)
            {
                foreach (var row in table.Rows)
                {
                    var prn = new ReconcileIssuedPrn
                    {
                        PrnNumber = row[prn_Number]?.ToString() ?? string.Empty,
                        PrnStatus = row[status]?.ToString() ?? string.Empty,
                        UploadedDate = row[report_Date]?.ToString() ?? string.Empty,
                        OrganisationName = row[org_Name]?.ToString() ?? string.Empty
                    };

                    prns.Add(prn);
                }
            }
        }

        return prns;
    }
}
