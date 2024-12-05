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
                var existingMessage = await ReceiveDeltaSyncExecutionFromQueue(deltaSyncExecution.SyncType);

                logger.LogInformation(
                    existingMessage != null
                        ? "SendDeltaSyncExecutionToQueue - Updated existing message for SyncType: {SyncType} in the queue"
                        : "SendDeltaSyncExecutionToQueue - Created new message for SyncType: {SyncType} in the queue",
                    deltaSyncExecution.SyncType);

                await using var sender = serviceBusClient.CreateSender(config.Value.DeltaSyncQueueName);
                var executionMessage = JsonSerializer.Serialize(deltaSyncExecution);
                var message = new ServiceBusMessage(executionMessage)
                {
                    ContentType = "application/json"
                };
                
                await sender.SendMessageAsync(message);
                logger.LogInformation("SendDeltaSyncExecutionToQueue - A message has been published to the queue: {queue}", config.Value.DeltaSyncQueueName);
            }
            catch (Exception ex)
            {
                logger.LogError("SendDeltaSyncExecutionToQueue failed to add message on Queue with exception: {exception}", ex);
                throw;
            }
        }

        /// <summary>
        /// This is the Receiver for the messages in the delta sync queue
        /// </summary>
        /// <param name="syncType"></param>
        /// <returns></returns>
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
                            await receiver.CompleteMessageAsync(message);
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
