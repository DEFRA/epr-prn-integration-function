using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EprPrnIntegration.Common.Service
{
    public class ServiceBusProvider(
        ILogger<ServiceBusProvider> logger,
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusConfiguration> config) : IServiceBusProvider
    {
        public async Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns)
        {
            try
            {
                await using var sender = serviceBusClient.CreateSender(config.Value.FetchPrnQueueName);
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
                foreach (var prn in prns)
                {
                    var jsonPrn = JsonSerializer.Serialize(prn);
                    if (!messageBatch.TryAddMessage(new ServiceBusMessage(jsonPrn)))
                    {
                        logger.LogWarning("SendFetchedNpwdPrnsToQueue - The message with EvidenNo: {EvidenNo} is too large to fit in the batch.", prn.EvidenceNo);
                    }
                }
                await sender.SendMessagesAsync(messageBatch);
                logger.LogInformation("SendFetchedNpwdPrnsToQueue - A batch of {MessageBatchCount} messages has been published to the queue: {queue}", messageBatch.Count, config.Value.FetchPrnQueueName);
            }
            catch (Exception ex)
            {
                logger.LogError("SendFetchedNpwdPrnsToQueue failed to add message on Queue with exception: {exception}", ex);
                throw;
            }
        }

        public async Task SendDeltaSyncExecutionToQueue(DeltaSyncExecution deltaSyncExecution)
        {
            try
            {
                await using var sender = serviceBusClient.CreateSender(config.Value.DeltaSyncQueueName);
                using var messageBatch = await sender.CreateMessageBatchAsync();

                var executionMessage = JsonSerializer.Serialize(deltaSyncExecution);
                var message = new ServiceBusMessage(executionMessage)
                {
                    ContentType = "application/json"
                };

                var existingMessage = await ReceiveDeltaSyncExecutionFromQueue(deltaSyncExecution.SyncType);

                if (existingMessage != null)
                {
                    if (!messageBatch.TryAddMessage(message))
                    {
                        logger.LogWarning("SendDeltaSyncExecutionToQueue - The updated message for SyncType: {SyncType} is too large to fit in the batch.", deltaSyncExecution.SyncType);
                    }
                    logger.LogInformation("SendDeltaSyncExecutionToQueue - Updated existing message for SyncType: {SyncType} in the queue", deltaSyncExecution.SyncType);
                }
                else
                {
                    if (!messageBatch.TryAddMessage(message))
                    {
                        logger.LogWarning("SendDeltaSyncExecutionToQueue - The new message for SyncType: {SyncType} is too large to fit in the batch.", deltaSyncExecution.SyncType);
                    }
                    logger.LogInformation("SendDeltaSyncExecutionToQueue - Created new message for SyncType: {SyncType} in the queue", deltaSyncExecution.SyncType);
                }

                await sender.SendMessagesAsync(messageBatch);
                logger.LogInformation("SendDeltaSyncExecutionToQueue - A batch of {MessageBatchCount} messages has been published to the queue: {queue}", messageBatch.Count, config.Value.DeltaSyncQueueName);
            }
            catch (Exception ex)
            {
                logger.LogError("SendDeltaSyncExecutionToQueue failed to add message on Queue with exception: {exception}", ex);
                throw;
            }
        }

        public async Task<DeltaSyncExecution?> ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType syncType)
        {
            try
            {
                await using var receiver = serviceBusClient.CreateReceiver(config.Value.DeltaSyncQueueName);

                var messages = await receiver.ReceiveMessagesAsync(maxMessages: 5, maxWaitTime: TimeSpan.FromSeconds(10));

                if (messages == null || !messages.Any())
                {
                    logger.LogInformation("No messages received from the queue: {queue}", config.Value.DeltaSyncQueueName);
                    return null;
                }
               
                foreach (var message in messages)
                {
                    try
                    {
                        var deltaSync = JsonSerializer.Deserialize<DeltaSyncExecution>(message.Body.ToString());

                        if (deltaSync != null && deltaSync.SyncType == syncType)
                        {
                            return deltaSync;
                        }

                        await receiver.AbandonMessageAsync(message);
                    }
                    catch (Exception deserializationEx)
                    {
                        logger.LogError("Error deserializing message: {exception}", deserializationEx);
                        await receiver.AbandonMessageAsync(message);
                    }
                }

                // no message is found with the expected type and hence return null
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError("ReceiveDeltaSyncExecutionFromQueue failed with exception: {exception}", ex);
                throw; 
            }
        }
    }
}
