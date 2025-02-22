﻿using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;

namespace EprPrnIntegration.Common.Service
{
    public interface IServiceBusProvider
    {
        Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns);
        Task SendMessageToErrorQueue(ServiceBusReceivedMessage receivedMessage, string evidenceNo);
        Task SendDeltaSyncExecutionToQueue(DeltaSyncExecution deltaSyncExecution);
        Task<DeltaSyncExecution?> GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType syncType);
        Task<List<T>> ProcessFetchedPrns<T>(Func<ServiceBusReceivedMessage, Task<T?>> messageHandler);
    }
}
