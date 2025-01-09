using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Helpers;

public class Utilities(IServiceBusProvider serviceBusProvider, IConfiguration configuration, TelemetryClient telemetryClient) : IUtilities
{
    public async Task<DeltaSyncExecution> GetDeltaSyncExecution(NpwdDeltaSyncType syncType)
    {
        var deltaMessage = await serviceBusProvider.GetDeltaSyncExecutionFromQueue(syncType);

        if (deltaMessage != null)
        {
            // To increase the expiry of the message
            // We complete the message on retrieval, push back to avoid loss in case of execution failure.
            await serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaMessage);
        }
        
        deltaMessage ??= new DeltaSyncExecution
        {
            LastSyncDateTime = DateTime.Parse(configuration["DefaultLastRunDate"]),
            SyncType = syncType
        };

        return deltaMessage;
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

    public async Task<Stream> CreateErrorEventsCsvStreamAsync(List<ErrorEvent> errorEvents)
    {
        var stream = new MemoryStream();

        if (errorEvents?.Count > 0)
        {
            await using var writer = new StreamWriter(stream, leaveOpen: true);

            await writer.WriteCsvCellAsync("PRN Number");
            await writer.WriteCsvCellAsync("Incoming Status");
            await writer.WriteCsvCellAsync("Date");
            await writer.WriteCsvCellAsync("Organisation Name");
            await writer.WriteCsvCellAsync("Error Comments", true);
            await writer.WriteLineAsync();

            foreach (var errorEvent in errorEvents)
            {
                await writer.WriteCsvCellAsync(errorEvent.PrnNumber);
                await writer.WriteCsvCellAsync(errorEvent.IncomingStatus);
                await writer.WriteCsvCellAsync(errorEvent.Date);
                await writer.WriteCsvCellAsync(errorEvent.OrganisationName);
                await writer.WriteCsvCellAsync(errorEvent.ErrorComments, true);
                await writer.WriteLineAsync();
            }

            await writer.FlushAsync();
        }

        stream.Position = 0;
        return stream;
    }
}