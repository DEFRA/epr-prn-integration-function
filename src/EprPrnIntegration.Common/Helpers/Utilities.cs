using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Helpers;

public class Utilities(IServiceBusProvider serviceBusProvider, IConfiguration configuration) : IUtilities
{
    public async Task<DeltaSyncExecution> GetDeltaSyncExecution(NpwdDeltaSyncType syncType)
    {
        var deltaMessage =
            await serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(syncType);
        return deltaMessage ?? new DeltaSyncExecution
        {
            LastSyncDateTime = DateTime.Parse(configuration["DefaultLastRunDate"]),
            SyncType = syncType
        };
    }

    public async Task SetDeltaSyncExecution(DeltaSyncExecution syncExecution, DateTime latestRun)
    {
        syncExecution.LastSyncDateTime = latestRun;
        await serviceBusProvider.SendDeltaSyncExecutionToQueue(syncExecution);
    }
}