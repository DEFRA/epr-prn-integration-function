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
            ServiceBusMessageBatch? messageBatch = null;
            try
            {
                await using var sender = serviceBusClient.CreateSender(config.Value.FetchPrnQueueName);
                messageBatch = await sender.CreateMessageBatchAsync();
                foreach (var prn in prns)
                {
                    var jsonPrn = JsonSerializer.Serialize(prn);
                    var message = new ServiceBusMessage(jsonPrn);
                    if (!messageBatch.TryAddMessage(message))
                    {
                        logger.LogInformation("SendFetchedNpwdPrnsToQueue - Batch overflow sending this batch with message count {count}", messageBatch.Count);
                        await sender.SendMessagesAsync(messageBatch);

                        logger.LogInformation("SendFetchedNpwdPrnsToQueue - Disposing current batch and creating new batch");
                        messageBatch.Dispose();
                        messageBatch = await sender.CreateMessageBatchAsync();

                        logger.LogInformation("SendFetchedNpwdPrnsToQueue - Adding message in new batch");
                        if (!messageBatch.TryAddMessage(message))
                        {
                            throw new InvalidOperationException("SendFetchedNpwdPrnsToQueue - Could not add message into new batch");
                        }
                    }
                }
                if (messageBatch.Count > 0)
                {
                    logger.LogInformation("SendFetchedNpwdPrnsToQueue - Sending final batch with message count {count}", messageBatch.Count);
                    await sender.SendMessagesAsync(messageBatch);
                }
                logger.LogInformation("SendFetchedNpwdPrnsToQueue - total {count} messages has been published to the queue: {queue}", messageBatch.Count, config.Value.FetchPrnQueueName);
            }
            catch (Exception ex)
            {
                logger.LogError("SendFetchedNpwdPrnsToQueue failed to add message on Queue with exception: {exception}", ex);
                throw;
            }
            finally
            {
                messageBatch?.Dispose();
            }
        }

        public async Task SendDeltaSyncExecutionToQueue(DeltaSyncExecution deltaSyncExecution)
        {
            try
            {
                var queueName = GetDeltaSyncQueueName(deltaSyncExecution.SyncType);

                await using var sender = serviceBusClient.CreateSender(queueName);
                var executionMessage = JsonSerializer.Serialize(deltaSyncExecution);
                var message = new ServiceBusMessage(executionMessage)
                {
                    ContentType = "application/json"
                };
                
                await sender.SendMessageAsync(message);
                logger.LogInformation("SendDeltaSyncExecutionToQueue - A message has been published to the queue: {queue}", queueName);
            }
            catch (Exception ex)
            {
                logger.LogError("SendDeltaSyncExecutionToQueue failed to add message on Queue with exception: {exception}", ex);
                throw;
            }
        }
        
        public async Task<DeltaSyncExecution?> GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType syncType)
        {
            var queueName = GetDeltaSyncQueueName(syncType);
            try
            {
                await using var receiver = serviceBusClient.CreateReceiver(queueName);

                var messages = await receiver.ReceiveMessagesAsync(int.MaxValue, TimeSpan.FromSeconds(config.Value.MaxWaitTimeInSeconds ?? 1));

                if (messages == null || messages.Count == 0)
                {
                    logger.LogInformation("No message received from the queue: {queue}", queueName);
                    return null;
                }

                var latestMessage = messages.OrderByDescending(m => m.SequenceNumber).First();
                var deltaSync = DeserializeMessage<DeltaSyncExecution>(latestMessage.Body.ToString());

                foreach (var message in messages)
                {
                    await receiver.CompleteMessageAsync(message);
                }

                return deltaSync;
            }
            catch (Exception ex)
            {
                logger.LogError("GetDeltaSyncExecutionFromQueue failed with exception: {exception}", ex);
                throw;
            }
        }

        public async Task SendMessageToErrorQueue(ServiceBusReceivedMessage receivedMessage, string evidenceNo)
        {
            try
            {
                await using var errorQueueSender = serviceBusClient.CreateSender(config.Value.ErrorPrnQueue);

                var errorMessage = new ServiceBusMessage(receivedMessage.Body)
                {
                    ContentType = receivedMessage.ContentType,
                    MessageId = receivedMessage.MessageId,
                    CorrelationId = receivedMessage.CorrelationId,
                    Subject = receivedMessage.Subject,
                    To = receivedMessage.To
                };

                foreach (var property in receivedMessage.ApplicationProperties)
                {
                    errorMessage.ApplicationProperties.Add(property.Key, property.Value);
                }

                await errorQueueSender.SendMessageAsync(errorMessage);
                logger.LogInformation("Message with EvidenceNo: {EvidenceNo} sent to error queue.", evidenceNo);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to send message to error queue with exception: {ExceptionMessage}", ex.Message);
            }
        }

        public async Task<List<T>> ProcessFetchedPrns<T>(Func<ServiceBusReceivedMessage, Task<T?>> messageHandler)
        {
            var invalidPrns = new List<T>();

            try
            {
                await using var receiver = serviceBusClient.CreateReceiver(config.Value.FetchPrnQueueName, new ServiceBusReceiverOptions() { ReceiveMode = ServiceBusReceiveMode.PeekLock });

                while (true)
                {
                    var messages = await receiver.ReceiveMessagesAsync(100, TimeSpan.FromSeconds(config.Value.MaxWaitTimeInSeconds ?? 1));
                    if (!messages.Any())
                    {
                        logger.LogInformation("No messages found in the queue. Exiting the processing loop.");
                        break;
                    }
                    foreach (var message in messages)
                    {
                        try
                        {
                            var validationFailedPrn = await messageHandler(message)!;
                            if (validationFailedPrn != null)
                            {
                                invalidPrns.Add(validationFailedPrn);
                            }
                            await receiver.CompleteMessageAsync(message);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to process message with id: {MessageId}", message.MessageId);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to receive messages from queue: {QueueName}", config.Value.FetchPrnQueueName);
            }

            return invalidPrns;
        }

        private string GetDeltaSyncQueueName(NpwdDeltaSyncType syncType) =>
            syncType switch
            {
                NpwdDeltaSyncType.UpdatedProducers => config.Value.UpdateProducerDeltaSyncQueueName,
                NpwdDeltaSyncType.UpdatePrns => config.Value.UpdatePrnDeltaSyncQueueName,
                NpwdDeltaSyncType.FetchNpwdIssuedPrns => config.Value.FetchPrnDeltaSyncQueueName,
                _ => throw new ArgumentOutOfRangeException(nameof(syncType), syncType, null)
            };

        private T? DeserializeMessage<T>(string messageBody)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(messageBody);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize message body: {MessageBody}", messageBody);
                return default;
            }
        }
    }
}
