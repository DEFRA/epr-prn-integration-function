using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.Service
{
    public interface IServiceBusProvider
    {
        Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns);
        Task<List<NpwdPrn>> ReceiveFetchedNpwdPrnsFromQueue(DateTime? lastRunDateTime, DateTime processDate);
    }
}
