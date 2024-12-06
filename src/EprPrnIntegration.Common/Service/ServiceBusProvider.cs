using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text.Json;

namespace EprPrnIntegration.Common.Service
{
    public class ServiceBusProvider : IServiceBusProvider
    {
        private readonly ILogger<ServiceBusProvider> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IOptions<ServiceBusConfiguration> _serviceBusConfig;
        public ServiceBusProvider(ILogger<ServiceBusProvider> logger, ServiceBusClient serviceBusClient, IOptions<ServiceBusConfiguration> config)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
            _serviceBusConfig = config;
        }
        public async Task SendFetchedNpwdPrnsToQueue(List<NpwdPrn> prns)
        {
            try
            {
                await using var sender = _serviceBusClient.CreateSender(_serviceBusConfig.Value.FetchPrnQueueName);
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
                foreach (var prn in prns)
                {
                    var jsonPrn = System.Text.Json.JsonSerializer.Serialize(prn);
                    if (!messageBatch.TryAddMessage(new ServiceBusMessage(jsonPrn)))
                    {
                        _logger.LogWarning("SendFetchedNpwdPrnsToQueue - The message with EvidenNo: {EvidenNo} is too large to fit in the batch.", prn.EvidenceNo);
                    }
                }
                await sender.SendMessagesAsync(messageBatch);
                _logger.LogInformation("SendFetchedNpwdPrnsToQueue - A batch of {MessageBatchCount} messages has been published to the queue: {queue}", messageBatch.Count, _serviceBusConfig.Value.FetchPrnQueueName);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendFetchedNpwdPrnsToQueue failed to add message on Queue with exception: {exception}", ex);
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
