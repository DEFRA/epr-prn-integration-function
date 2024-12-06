using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
                    var jsonPrn = System.Text.Json.JsonSerializer.Serialize(prn);
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

        public async Task<IEnumerable<ServiceBusReceivedMessage>> ReceiveFetchedNpwdPrnsFromQueue()
        {
            try
            {
                await using var receiver = _serviceBusClient.CreateReceiver(_serviceBusConfig.Value.FetchPrnQueueName, new ServiceBusReceiverOptions() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

                // Continue receiving messages until the queue is empty or we have processed the relevant range
                var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, _serviceBusConfig.Value.MaxWaitTime); //TODO: maxMessages change to int.MaxValue after testing

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages from queue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }

        public async Task SendMessageBackToFetchPrnQueue(ServiceBusReceivedMessage receivedMessage)
        {
            try
            {
                await using var sender = _serviceBusClient.CreateSender(_serviceBusConfig.Value.FetchPrnQueueName);

                var retryMessage = new ServiceBusMessage(receivedMessage.Body)
                {
                    ContentType = receivedMessage.ContentType,
                    MessageId = receivedMessage.MessageId,
                    CorrelationId = receivedMessage.CorrelationId,
                    Subject = receivedMessage.Subject,
                    To = receivedMessage.To
                };

                // Copy over the application properties to ensure message state is preserved.
                foreach (var property in receivedMessage.ApplicationProperties)
                {
                    retryMessage.ApplicationProperties.Add(property.Key, property.Value);
                }

                // Send the message back to the FetchPrnQueue.
                await sender.SendMessageAsync(retryMessage);

                var evidence = JsonConvert.DeserializeObject<Evidence>(receivedMessage.Body.ToString());
                _logger.LogInformation("Message with EvidenceNo: {EvidenceNo} sent back to the FetchPrnQueue.", evidence?.EvidenceNo);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send message back to FetchPrnQueue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }

        public async Task SendMessageToErrorQueue(ServiceBusReceivedMessage receivedMessage)
        {
            try
            {
                await using var errorQueueSender = _serviceBusClient.CreateSender(_serviceBusConfig.Value.ErrorPrnQueue);

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

                var evidence = JsonConvert.DeserializeObject<Evidence>(receivedMessage.Body.ToString());
                _logger.LogInformation("Message with EvidenceNo: {EvidenceNo} sent to error queue.", evidence?.EvidenceNo);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send message to error queue with exception: {ExceptionMessage}", ex.Message);
            }
        }
    }
}
