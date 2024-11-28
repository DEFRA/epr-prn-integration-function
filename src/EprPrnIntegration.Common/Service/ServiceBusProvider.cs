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
            try
            {
                await using var sender = _serviceBusClient.CreateSender(_serviceBusConfig.Value.FetchPrnQueueName);
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
                foreach (var prn in prns)
                {
                    var jsonPrn = JsonSerializer.Serialize(prn);
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
    }
}
