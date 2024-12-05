using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Helpers;

public class Utilities(IServiceBusProvider serviceBusProvider, IConfiguration configuration) : IUtilities
{
    public async Task<DeltaSyncExecution> GetDeltaSyncExecution()
    {
        var deltaMessage =
            await serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);
        return deltaMessage ?? new DeltaSyncExecution
        {
            LastSyncDateTime = DateTime.Parse(configuration["DefaultLastRunDate"]),
            SyncType = NpwdDeltaSyncType.UpdatedProducers
        };
    }

    public async Task SetDeltaSyncExecution(DeltaSyncExecution syncExecution, DateTime latestRun)
    {
        syncExecution.LastSyncDateTime = latestRun;
        await serviceBusProvider.SendDeltaSyncExecutionToQueue(syncExecution);
    }
}