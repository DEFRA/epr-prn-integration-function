using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.Service
{
    public interface IServiceBusProvider
    {
        Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns);
        Task<IEnumerable<ServiceBusReceivedMessage>> ReceiveFetchedNpwdPrnsFromQueue();
        Task SendMessageToErrorQueue(ServiceBusReceivedMessage receivedMessage);
        Task SendMessageBackToFetchPrnQueue(ServiceBusReceivedMessage receivedMessage);
    }
}
