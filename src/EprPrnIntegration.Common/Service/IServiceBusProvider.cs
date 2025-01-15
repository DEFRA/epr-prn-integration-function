using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;

namespace EprPrnIntegration.Common.Service
{
    public interface IServiceBusProvider
    {
        Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns);
        Task<IEnumerable<ServiceBusReceivedMessage>> ReceiveFetchedNpwdPrnsFromQueue();
        Task SendMessageToErrorQueue(ServiceBusReceivedMessage receivedMessage, string evidenceNo);
        Task SendDeltaSyncExecutionToQueue(DeltaSyncExecution deltaSyncExecution);
        Task<DeltaSyncExecution?> GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType syncType);
        Task SendMessageBackToFetchPrnQueue(ServiceBusReceivedMessage receivedMessage, string evidenceNo);
    }
}
