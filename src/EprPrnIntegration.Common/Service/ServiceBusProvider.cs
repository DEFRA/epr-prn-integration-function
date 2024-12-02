using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            ServiceBusMessageBatch? messageBatch = null;
            try
            {
                await using var sender = _serviceBusClient.CreateSender(_serviceBusConfig.Value.FetchPrnQueueName);
                messageBatch = await sender.CreateMessageBatchAsync();
                foreach (var prn in prns)
                {
                    var jsonPrn = JsonSerializer.Serialize(prn);
                    var message = new ServiceBusMessage(jsonPrn);
                    if (!messageBatch.TryAddMessage(message))
                    {
                        _logger.LogInformation("SendFetchedNpwdPrnsToQueue - Batch overflow sending this batch with message count {count}", messageBatch.Count);
                        await sender.SendMessagesAsync(messageBatch);

                        _logger.LogInformation("SendFetchedNpwdPrnsToQueue - Disposing current batch and creating new batch");
                        messageBatch.Dispose();
                        messageBatch = await sender.CreateMessageBatchAsync();

                        _logger.LogInformation("SendFetchedNpwdPrnsToQueue - Adding message in new batch");
                        if (!messageBatch.TryAddMessage(message))
                        {
                            throw new InvalidOperationException("SendFetchedNpwdPrnsToQueue - Could not add message into new batch");
                        }
                    }
                }
                if (messageBatch.Count > 0)
                {
                    _logger.LogInformation("SendFetchedNpwdPrnsToQueue - Sending final batch with message count {count}", messageBatch.Count);
                    await sender.SendMessagesAsync(messageBatch);
                }
                _logger.LogInformation("SendFetchedNpwdPrnsToQueue - total {MessageBatchCount} messages has been published to the queue: {queue}", messageBatch.Count, _serviceBusConfig.Value.FetchPrnQueueName);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendFetchedNpwdPrnsToQueue failed to add message on Queue with exception: {exception}", ex);
                throw;
            }
            finally
            {
                messageBatch?.Dispose();
            }
        }
    }
}
