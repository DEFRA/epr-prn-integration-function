using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace EprPrnIntegration.Common.Helpers;

public class Utilities(IServiceBusProvider serviceBusProvider, IConfiguration configuration, TelemetryClient telemetryClient) 
    : IUtilities
{
    public async Task<DeltaSyncExecution> GetDeltaSyncExecution(NpwdDeltaSyncType syncType)
    {
        var deltaMessage = await serviceBusProvider.GetDeltaSyncExecutionFromQueue(syncType);

        if (deltaMessage != null)
        {
            // To increase the expiry of the message
            // We complete the message on retrieval, push back to avoid loss in case of execution failure.
            await serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaMessage);
            return deltaMessage;
        }
        
        return new DeltaSyncExecution
        {
            LastSyncDateTime = DateTime.Parse(configuration["DefaultLastRunDate"]),
            SyncType = syncType
        };
    }

    public async Task SetDeltaSyncExecution(DeltaSyncExecution syncExecution, DateTime latestRun)
    {
        // getting the message to make sure it gets removed from the queue, so send push a new one to the queue
        await serviceBusProvider.GetDeltaSyncExecutionFromQueue(syncExecution.SyncType);

        syncExecution.LastSyncDateTime = latestRun;
        await serviceBusProvider.SendDeltaSyncExecutionToQueue(syncExecution);
    }

    public void AddCustomEvent(string eventName, IDictionary<string, string> eventData)
    {
        telemetryClient.TrackEvent(eventName, eventData);
    }
    
    public string CreateCsvContent(Dictionary<string, List<string>> data)
    {
        var contentBuilder = new StringBuilder();
        
        contentBuilder.AppendLine(string.Join(",", data.Keys));

        var rowCount = data.Values.Max(values => values.Count);
        for (var i = 0; i < rowCount; i++)
        {
            var row = data.Keys.Select(key =>
            {
                var values = data[key];
                return i < values.Count ? values[i].CleanCsvString() : string.Empty;
            });

            contentBuilder.AppendLine(string.Join(",", row));
        }

        return contentBuilder.ToString();
    }
}