using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Validators;
using FluentValidation;
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

        public async Task<List<NpwdPrn>> ReceiveFetchedNpwdPrnsFromQueue(DateTime? lastRunDateTime, DateTime processDate)
        {
            var prns = new List<NpwdPrn>();

            try
            {
                await using var receiver = _serviceBusClient.CreateReceiver(_serviceBusConfig.Value.FetchPrnQueueName);

                // Continue receiving messages until the queue is empty or we have processed the relevant range
                while (true)
                {
                    // Receive a batch of 10 messages
                    var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(30));

                    if (messages.Count == 0)
                    {
                        // Exit the loop if no messages are received
                        break;
                    }

                    foreach (var message in messages)
                    {
                        try
                        {
                            // Deserialize the message as a generic object to check for LastRunDateTime type
                            var messageContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body.ToString());

                            if (messageContent != null && messageContent.ContainsKey("MessageType"))
                            {
                                var messageType = messageContent["MessageType"].ToString();

                                // If the message is of type LastRunDateTime, skip it (abandon the message)
                                if (messageType == "LastRunDateTime")
                                {
                                    // Skip this message as it's only a marker
                                    _logger.LogInformation("Skipped LastRunDateTime message.");
                                    await receiver.AbandonMessageAsync(message);
                                    continue; // Skip further processing for this message
                                }
                            }

                            // Process messages based on the supplied lastRunDateTime
                            if (lastRunDateTime.HasValue)
                            {
                                // Process only if the message timestamp is between lastRunDateTime and the processDate
                                var messageTimestamp = GetMessageTimestamp(message); // Venkat: need to define how to extract this from the message
                                if (messageTimestamp.HasValue && messageTimestamp >= lastRunDateTime.Value && messageTimestamp <= processDate)
                                {
                                    // Try to deserialize the message as NpwdPrn
                                    var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                                    if (prn != null)
                                    {
                                        //IValidator<NpwdPrn> npwdPrnValidator inject this and use below code to validate
                                        //var validationResult = _npwdPrnValidator.Validate(npwdIssuedPrn);
                                        //if (!validationResult.IsValid)
                                        //{
                                        //}

                                        if (IsValidPrn(prn))
                                        {
                                            prns.Add(prn);

                                            //
                                            _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                            // Complete the message so it's removed from the queue
                                            await receiver.CompleteMessageAsync(message);
                                        }
                                        else
                                        {
                                            await SendMessageToErrorQueue(message);
                                            // Abandon the invalid message to prevent it from being processed again
                                            await receiver.AbandonMessageAsync(message);
                                        }
                                    }
                                    else
                                    {
                                        // If it's not a valid NpwdPrn, abandon the message
                                        await receiver.AbandonMessageAsync(message);
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("Message timestamp {MessageTimestamp} is outside the range of {LastRunDateTime} and {ProcessDate}, skipping.",
                                                            messageTimestamp, lastRunDateTime.Value, processDate);
                                    await receiver.AbandonMessageAsync(message);
                                }
                            }
                            else
                            {
                                // If lastRunDateTime is not provided, process messages up to the processDate as this is the first run
                                var messageTimestamp = GetMessageTimestamp(message); // You need to define how to extract this from the message
                                if (messageTimestamp.HasValue && messageTimestamp <= processDate)
                                {
                                    // Try to deserialize the message as NpwdPrn
                                    var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                                    if (prn != null)
                                    {
                                        if (IsValidPrn(prn))
                                        {
                                            prns.Add(prn);
                                            _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                            // Complete the message so it's removed from the queue
                                            await receiver.CompleteMessageAsync(message);
                                        }
                                        else
                                        {
                                            await SendMessageToErrorQueue(message);
                                            // Abandon the invalid message to prevent it from being processed again
                                            await receiver.AbandonMessageAsync(message);
                                        }
                                    }
                                    else
                                    {
                                        // If it's not a valid NpwdPrn, abandon the message
                                        await receiver.AbandonMessageAsync(message);
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("Message timestamp {MessageTimestamp} is after the process date {ProcessDate}, skipping.",
                                                            messageTimestamp, processDate);
                                    await receiver.AbandonMessageAsync(message);
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError("Failed to deserialize message: {ExceptionMessage}", jsonEx.Message);
                            await receiver.AbandonMessageAsync(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error processing message: {ExceptionMessage}", ex.Message);
                            await receiver.AbandonMessageAsync(message);
                        }
                    }
                }

                if (prns.Count == 0)
                {
                    _logger.LogInformation("No valid PRN messages received from the queue.");
                }

                return prns;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages from queue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }

        public async Task<List<NpwdPrn>> ReceiveFetchedNpwdPrnsFromQueueToChange(DateTime? lastRunDateTime, DateTime processDate)
        {
            var prns = new List<NpwdPrn>();

            try
            {
                await using var receiver = _serviceBusClient.CreateReceiver(_serviceBusConfig.Value.FetchPrnQueueName);

                // Continue receiving messages until the queue is empty or we have processed the relevant range
                while (true)
                {
                    // Receive a batch of 10 messages
                    var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(30));

                    if (messages.Count == 0)
                    {
                        // Exit the loop if no messages are received
                        break;
                    }

                    foreach (var message in messages)
                    {
                        try
                        {
                            // Deserialize the message as a generic object to check for LastRunDateTime type
                            var messageContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body.ToString());

                            if (messageContent != null && messageContent.ContainsKey("MessageType"))
                            {
                                var messageType = messageContent["MessageType"].ToString();

                                // If the message is of type LastRunDateTime, skip it (abandon the message)
                                if (messageType == "LastRunDateTime")
                                {
                                    // Skip this message as it's only a marker
                                    _logger.LogInformation("Skipped LastRunDateTime message.");
                                    await receiver.AbandonMessageAsync(message);
                                    continue; // Skip further processing for this message
                                }
                            }

                            // Process messages based on the supplied lastRunDateTime
                            if (lastRunDateTime.HasValue)
                            {
                                // Process only if the message timestamp is between lastRunDateTime and the processDate
                                var messageTimestamp = GetMessageTimestamp(message); // Venkat: need to define how to extract this from the message
                                if (messageTimestamp.HasValue && messageTimestamp >= lastRunDateTime.Value && messageTimestamp <= processDate)
                                {
                                    // Try to deserialize the message as NpwdPrn
                                    var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                                    if (prn != null)
                                    {
                                        if (IsValidPrn(prn))
                                        {
                                            prns.Add(prn);

                                            //
                                            _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                            // Complete the message so it's removed from the queue
                                            await receiver.CompleteMessageAsync(message);
                                        }
                                        else
                                        {
                                            await SendMessageToErrorQueue(message);
                                            // Abandon the invalid message to prevent it from being processed again
                                            await receiver.AbandonMessageAsync(message);
                                        }
                                    }
                                    else
                                    {
                                        // If it's not a valid NpwdPrn, abandon the message
                                        await receiver.AbandonMessageAsync(message);
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("Message timestamp {MessageTimestamp} is outside the range of {LastRunDateTime} and {ProcessDate}, skipping.",
                                                            messageTimestamp, lastRunDateTime.Value, processDate);
                                    await receiver.AbandonMessageAsync(message);
                                }
                            }
                            else
                            {
                                // If lastRunDateTime is not provided, process messages up to the processDate as this is the first run
                                var messageTimestamp = GetMessageTimestamp(message); // You need to define how to extract this from the message
                                if (messageTimestamp.HasValue && messageTimestamp <= processDate)
                                {
                                    // Try to deserialize the message as NpwdPrn
                                    var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                                    if (prn != null)
                                    {
                                        if (IsValidPrn(prn))
                                        {
                                            prns.Add(prn);
                                            _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                            // Complete the message so it's removed from the queue
                                            await receiver.CompleteMessageAsync(message);
                                        }
                                        else
                                        {
                                            await SendMessageToErrorQueue(message);
                                            // Abandon the invalid message to prevent it from being processed again
                                            await receiver.AbandonMessageAsync(message);
                                        }
                                    }
                                    else
                                    {
                                        // If it's not a valid NpwdPrn, abandon the message
                                        await receiver.AbandonMessageAsync(message);
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("Message timestamp {MessageTimestamp} is after the process date {ProcessDate}, skipping.",
                                                            messageTimestamp, processDate);
                                    await receiver.AbandonMessageAsync(message);
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError("Failed to deserialize message: {ExceptionMessage}", jsonEx.Message);
                            await receiver.AbandonMessageAsync(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error processing message: {ExceptionMessage}", ex.Message);
                            await receiver.AbandonMessageAsync(message);
                        }
                    }
                }

                if (prns.Count == 0)
                {
                    _logger.LogInformation("No valid PRN messages received from the queue.");
                }

                return prns;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages from queue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }

        private DateTime? GetMessageTimestamp(ServiceBusReceivedMessage message)
        {
            try
            {
                // Deserialize the message body into a dictionary to access its fields
                var messageContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body.ToString());

                //Venkat: Which Timestamp we need to use ModifiedOn? or  StatusDate?
                if (messageContent != null && messageContent.ContainsKey("StatusDate"))
                {
                    DateTime timestamp;
                    if (DateTime.TryParse(messageContent["StatusDate"].ToString(), out timestamp))
                    {
                        return timestamp;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error extracting StatusDate timestamp from message: {ExceptionMessage}", ex.Message);
            }

            return null; // Return null if no timestamp is found or extraction fails
        }


        public async Task<List<NpwdPrn>> ReceiveFetchedNpwdPrnsFromQueueOLD3()
        {
            var prns = new List<NpwdPrn>();

            try
            {
                await using var receiver = _serviceBusClient.CreateReceiver(_serviceBusConfig.Value.FetchPrnQueueName);

                // Continue receiving messages until the queue is empty
                while (true)
                {
                    // Receive 10 messages as a batch
                    var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(30));

                    if (messages.Count == 0)
                    {
                        // Exit if no messages are received
                        break;
                    }

                    foreach (var message in messages)
                    {
                        try
                        {
                            // Try to deserialize the message as a generic object to check for LastRunDateTime message type
                            var messageContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body.ToString());

                            if (messageContent != null && messageContent.ContainsKey("MessageType") &&
                                messageContent["MessageType"].ToString() == "LastRunTimestamp")
                            {
                                // Process the LastRunDateTime message
                                if (TryGetLastRunDateTime(message, out DateTime lastRunDateTime))
                                {
                                    // Check if the LastRunDateTime is today
                                    if (lastRunDateTime.Date == DateTime.UtcNow.Date)
                                    {
                                        _logger.LogInformation("Processed LastRunDateTime message for today's date: {Date}", lastRunDateTime.Date);
                                        // Abandon the message to keep it in the queue
                                        await receiver.AbandonMessageAsync(message);
                                    }
                                    else
                                    {
                                        _logger.LogInformation("LastRunDateTime is not today, skipping this message.");
                                        await receiver.AbandonMessageAsync(message); // Abandon if it's not today's date
                                    }
                                }
                                else
                                {
                                    // If deserialization fails for LastRunDateTime, abandon the message
                                    await receiver.AbandonMessageAsync(message);
                                }

                                continue; // Skip to the next message
                            }

                            // Now try to deserialize the message as NpwdPrn
                            var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                            if (prn != null)
                            {
                                if (IsValidPrn(prn))
                                {
                                    prns.Add(prn);
                                    _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                    // Complete the message so it's removed from the queue
                                    await receiver.CompleteMessageAsync(message);
                                }
                                else
                                {
                                    await SendMessageToErrorQueue(message);
                                    // Abandon the invalid message to prevent it from being processed again
                                    await receiver.AbandonMessageAsync(message);
                                }
                            }
                            else
                            {
                                // If it's not a valid NpwdPrn, abandon the message
                                await receiver.AbandonMessageAsync(message);
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError("Failed to deserialize message: {ExceptionMessage}", jsonEx.Message);
                            await receiver.AbandonMessageAsync(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error processing message: {ExceptionMessage}", ex.Message);
                            await receiver.AbandonMessageAsync(message);
                        }
                    }
                }

                if (prns.Count == 0)
                {
                    _logger.LogInformation("No valid PRN messages received from the queue.");
                }

                return prns;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages from queue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }

        private bool TryGetLastRunDateTime(ServiceBusReceivedMessage message, out DateTime lastRunDateTime)
        {
            try
            {
                // Deserialize the message as LastRunDateTime (it could be a different format)
                var messageContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body.ToString());

                if (messageContent != null && messageContent.ContainsKey("LastRunDateTime") &&
                    DateTime.TryParse(messageContent["LastRunDateTime"].ToString(), out lastRunDateTime))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error extracting LastRunDateTime from message: {ExceptionMessage}", ex.Message);
            }

            lastRunDateTime = default;
            return false;
        }


        public async Task<List<NpwdPrn>> ReceiveFetchedNpwdPrnsFromQueueOLD2()
        {
            var prns = new List<NpwdPrn>();

            try
            {
                await using var receiver = _serviceBusClient.CreateReceiver(_serviceBusConfig.Value.FetchPrnQueueName);
                // Venkat: Setting to Max 10 messages at once
                var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(30)); 

                foreach (var message in messages)
                {
                    try
                    {
                        // Deserialize the message as a generic object first to check for LastRunDateTime message type
                        var messageContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body.ToString());

                        if (messageContent != null && messageContent.ContainsKey("MessageType") &&
                            messageContent["MessageType"].ToString() == "LastRunTimestamp")
                        {
                            // This message is for the LastRunDateTime, so skip it but don't complete it
                            _logger.LogInformation("Skipped LastRunDateTime message.");
                            // Abandon the message so it remains in the queue for future processing
                            await receiver.AbandonMessageAsync(message);
                            continue;
                        }

                        // Now try to deserialize the message as NpwdPrn
                        var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                        if (prn != null)
                        {
                            if (IsValidPrn(prn))
                            {
                                prns.Add(prn);
                                _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                // Complete the message so it's removed from the queue
                                await receiver.CompleteMessageAsync(message);
                            }
                            else
                            {
                                await SendMessageToErrorQueue(message);
                                // Abandon the invalid message to prevent it from being processed again
                                await receiver.AbandonMessageAsync(message);
                            }
                        }
                        else
                        {
                            // If it's not a valid NpwdPrn, abandon the message
                            await receiver.AbandonMessageAsync(message);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError("Failed to deserialize message: {ExceptionMessage}", jsonEx.Message);
                        await receiver.AbandonMessageAsync(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error processing message: {ExceptionMessage}", ex.Message);
                        await receiver.AbandonMessageAsync(message);
                    }
                }

                if (prns.Count == 0)
                {
                    _logger.LogInformation("No messages received from the queue.");
                }

                return prns;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages from queue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }


        public async Task<List<NpwdPrn>> ReceiveFetchedNpwdPrnsFromQueueOLD()
        {
            var prns = new List<NpwdPrn>();

            try
            {
                await using var receiver = _serviceBusClient.CreateReceiver(_serviceBusConfig.Value.FetchPrnQueueName);
                var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(30)); // Max 10 messages at once

                foreach (var message in messages)
                {
                    try
                    {
                        var prn = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                        if (prn != null)
                        {
                            if (IsValidPrn(prn))
                            {
                                prns.Add(prn);
                                _logger.LogInformation("Received and validated message for EvidenceNo: {EvidenceNo}", prn.EvidenceNo);
                                //Venkat: Is this ok
                                // Complete the message so that it is removed from the queue
                                await receiver.CompleteMessageAsync(message);
                            }
                            else
                            {
                                await SendMessageToErrorQueue(message);
                                //Venkat: Is this ok to delete from the queue? Done below as well....
                                // Abandon the invalid message to prevent it from being processed again
                                await receiver.AbandonMessageAsync(message);
                            }
                        }
                        await receiver.CompleteMessageAsync(message);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError("Failed to deserialize message: {ExceptionMessage}", jsonEx.Message);
                        await receiver.AbandonMessageAsync(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error processing message: {ExceptionMessage}", ex.Message);
                        await receiver.AbandonMessageAsync(message);
                    }
                }

                if (prns.Count == 0)
                {
                    _logger.LogInformation("No messages received from the queue.");
                }

                return prns;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages from queue with exception: {ExceptionMessage}", ex.Message);
                throw;
            }
        }

        public async Task UpdateLastRunDateTimeToQueue()
        {
            try
            {
                var lastRunDateTime = DateTime.UtcNow;

                // Create a message with a specific "MessageType" for the last run timestamp
                var lastRunMessage = new
                {
                    MessageType = "LastRunTimestamp",
                    LastRunDateTime = lastRunDateTime
                };

                var jsonLastRun = JsonSerializer.Serialize(lastRunMessage);

                // Use the same queue to send the message
                await using var sender = _serviceBusClient.CreateSender(_serviceBusConfig.Value.FetchPrnQueueName);
                var message = new ServiceBusMessage(jsonLastRun);
                await sender.SendMessageAsync(message);

                _logger.LogInformation("UpdateLastRunDateTimeToQueue - The last run datetime ({LastRunDateTime}) has been published to the queue: {queue}",
                    lastRunDateTime, _serviceBusConfig.Value.FetchPrnQueueName);
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateLastRunDateTimeToQueue failed to send the datetime with exception: {exception}", ex);
                throw;
            }
        }

        // Helper class for deserializing the message
        private class MessageContent
        {
            public string MessageType { get; set; }
            public DateTime LastRunDateTime { get; set; }
        }


        private bool IsValidPrn(NpwdPrn prn)
        {
            if (string.IsNullOrWhiteSpace(prn.EvidenceNo))
            {
                _logger.LogWarning("Invalid PRN: EvidenceNo is missing.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(prn.IssuedToEPRId))
            {
                _logger.LogWarning("Invalid PRN: OrganisationId is missing.");
                return false;
            }

            return true;
        }

        private async Task SendMessageToErrorQueue(ServiceBusReceivedMessage receivedMessage)
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

                _logger.LogInformation("Message with EvidenceNo: {EvidenceNo} sent to error queue.", receivedMessage.ApplicationProperties["EvidenceNo"]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send message to error queue with exception: {ExceptionMessage}", ex.Message);
            }
        }
    }
}
