using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;

namespace EprPrnIntegration.Common.Service
{
    public interface IServiceBusProvider
    {
        Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns);
        Task SendDeltaSyncExecutionToQueue(DeltaSyncExecution deltaSyncExecutions);
        Task<DeltaSyncExecution?> ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType syncType);
    }
}
