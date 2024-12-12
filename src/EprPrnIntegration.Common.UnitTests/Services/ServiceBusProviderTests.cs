using System.Text.Json;
using AutoFixture;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Services
{
    public class ServiceBusProviderTests
    {
        private Mock<ILogger<ServiceBusProvider>> _loggerMock;
        private Mock<ServiceBusClient> _serviceBusClientMock;
        private Mock<IOptions<ServiceBusConfiguration>> _configMock;
        private Mock<ServiceBusSender> _serviceBusSenderMock;
        private ServiceBusProvider _serviceBusProvider;
        private Mock<ServiceBusReceiver> _serviceBusReceiverMock;
        private Fixture fixture;

        public ServiceBusProviderTests()
        {
            fixture = new Fixture();
            _loggerMock = new Mock<ILogger<ServiceBusProvider>>();
            _serviceBusClientMock = new Mock<ServiceBusClient>();
            _configMock = new Mock<IOptions<ServiceBusConfiguration>>();
            _serviceBusSenderMock = new Mock<ServiceBusSender>();
            _serviceBusReceiverMock = new Mock<ServiceBusReceiver>();

            var config = new ServiceBusConfiguration
            {
                FetchPrnQueueName = "test-queue-1",
                ErrorPrnQueue = "test-queue-2"
            };

            _configMock.Setup(c => c.Value).Returns(config);

            // Set up ServiceBusClient to return the mock receiver
            _serviceBusClientMock
                .Setup(client => client.CreateReceiver(It.IsAny<string>()))
                .Returns(_serviceBusReceiverMock.Object);

            _serviceBusProvider = new ServiceBusProvider(
                _loggerMock.Object,
                _serviceBusClientMock.Object,
                _configMock.Object
            );
        }

        [Fact]
        public async Task SendFetchedNpwdPrnsToQueue_SuccessfullyPushMessage()
        {
            var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(500, []);

            _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

            var npwdPrns = fixture.CreateMany<NpwdPrn>().ToList();

            await _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns);

            _serviceBusSenderMock.Verify(sender => sender.CreateMessageBatchAsync(default), Times.Once);
            _serviceBusSenderMock.Verify(sender => sender.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), default), Times.Once);
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(l => l.LogInformation(It.IsAny<string>()), Times.Exactly(2));

        }

        [Fact]
        public async Task SendFetchedPrnsToQueueAsync_SendBatchAndCreateNewAndAddMessage()
        {
            // Arrange
            var npwdPrns = fixture.CreateMany<NpwdPrn>(10).ToList();
            int messageCountThreshold = 6;
            List<ServiceBusMessage> messageList1 = [];
            List<ServiceBusMessage> messageList2 = [];
            ServiceBusMessageBatch messageBatch1 = ServiceBusModelFactory.ServiceBusMessageBatch(
                batchSizeBytes: 10 * 1024 *1024,
                batchMessageStore: messageList1,
                batchOptions: new CreateMessageBatchOptions(),
                tryAddCallback: _ => messageList1.Count < messageCountThreshold);
            ServiceBusMessageBatch messageBatch2 = ServiceBusModelFactory.ServiceBusMessageBatch(
                batchSizeBytes: 10 * 1024 * 1024,
                batchMessageStore: messageList2,
                batchOptions: new CreateMessageBatchOptions(),
                tryAddCallback: _ => messageList2.Count < messageCountThreshold);
            _serviceBusSenderMock.SetupSequence(sender => sender.CreateMessageBatchAsync(default))
                .ReturnsAsync(messageBatch1)
                .ReturnsAsync(messageBatch2);
            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            // Act
            await _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns);

            // Assert
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Exactly(1));
            _serviceBusSenderMock.Verify(sender => sender.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), default), Times.Exactly(2));
            _serviceBusSenderMock.Verify(sender => sender.CreateMessageBatchAsync(default), Times.Exactly(2));
            messageBatch1.Count.Should().Be(6);
            messageBatch2.Count.Should().Be(4);
        }

        [Fact]
        public async Task SendApprovedSubmissionsToQueueAsync_ShouldThrowError_WHenClientFails()
        {
            // Arrange
            var npwdPrns = fixture.CreateMany<NpwdPrn>().ToList();

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("error"));
            // Act

            await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns));

            // Assert
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendDeltaSyncExecutionToQueue_ExistingMessage_LogsUpdateAndSendsMessage()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };
            var existingMessage = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

            // Mocking ReceiveDeltaSyncExecutionFromQueue to return an existing message
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(System.Text.Json.JsonSerializer.Serialize(existingMessage)));

            _serviceBusReceiverMock.Setup(receiver => receiver
                .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message });

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

            // Act
            await _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution);

            // Assert: Ensure that SendMessageAsync was called with the correct message
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("Updated existing message for SyncType"))), Times.Once);

        }

        [Fact]
        public async Task SendDeltaSyncExecutionToQueue_NoExistingMessage_LogsCreateAndSendsMessage()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

            // Mocking ReceiveDeltaSyncExecutionFromQueue to return null (no existing message)
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

            _serviceBusReceiverMock.Setup(receiver => receiver
                    .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ServiceBusReceivedMessage>)null!);

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            // Act
            await _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution);

            // Assert: Ensure that SendMessageAsync was called with the correct message
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("Created new message for SyncType"))), Times.Once);
        }

        [Fact]
        public async Task SendDeltaSyncExecutionToQueue_ThrowsException_LogsError()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

            // Mock ReceiveDeltaSyncExecutionFromQueue to return a valid response
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

            _serviceBusReceiverMock.Setup(receiver => receiver
                    .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ServiceBusReceivedMessage>)null!);

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("error"));

            // Act & Assert: Ensure that an exception is thrown and error is logged
            await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution));
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("SendDeltaSyncExecutionToQueue failed to add message on Queue with exception"))), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_NoMessages_ReturnsNull()
        {
            // Arrange
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ServiceBusReceivedMessage>().ToList());

            // Act
            var result = await _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

            // Assert
            Assert.Null(result);
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("No messages received from the queue"))), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_MessageFound_DeserializedAndCompleted()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow
            };
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(JsonSerializer.Serialize(deltaSyncExecution)));

            // Mock ReceiveDeltaSyncExecutionFromQueue to return a valid response
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

            _serviceBusReceiverMock.Setup(receiver => receiver
                    .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message });


            _serviceBusReceiverMock.Setup(receiver =>
                    receiver.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deltaSyncExecution.SyncType, result.SyncType);
            _serviceBusReceiverMock.Verify(
                receiver => receiver.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_DeserializationFails_LogsErrorAndAbandonsMessage()
        {
            // Arrange
            var invalidMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(JsonSerializer.Serialize("invalid message")));

            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([invalidMessage]);

            _serviceBusReceiverMock.Setup(receiver =>
                    receiver.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                        It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

            // Assert
            Assert.Null(result);
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Error deserializing message"))), Times.Once);
            _serviceBusReceiverMock.Verify(
                receiver => receiver.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                    It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_ExceptionThrown_LogsErrorAndRethrows()
        {
            // Arrange
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service bus connection error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers));
            Assert.Equal("Service bus connection error", exception.Message);
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("ReceiveDeltaSyncExecutionFromQueue failed with exception"))), Times.Once);
        }

        [Fact]
        public async Task SendFetchedNpwdPrnsToQueue_MessageTooLarge_Warns()
        {
            // Arrange
            var npwdPrns = fixture.CreateMany<NpwdPrn>(10).ToList();
            int messageCountThreshold = 1;
            List<ServiceBusMessage> messageList = [new ServiceBusMessage()];

            ServiceBusMessageBatch messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(
                batchSizeBytes: 500,
                batchMessageStore: messageList,
                batchOptions: new CreateMessageBatchOptions(),
                tryAddCallback: _ => messageList.Count < messageCountThreshold);

            _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns));

            // Assert
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _serviceBusSenderMock.Verify(r => r.CreateMessageBatchAsync(default), Times.Exactly(2));

            _loggerMock.VerifyLog(l => l.LogError(It.Is<string>(s => s.Contains("SendFetchedNpwdPrnsToQueue failed to add message on Queue with exception"))), Times.Once);
        }

        [Fact]
        public async Task SendFetchedNpwdPrnsToQueue_ShouldThrowError_WhenClientFails()
        {
            // Arrange
            var npwdPrns = fixture.CreateMany<NpwdPrn>().ToList();

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("ServiceBus error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns));

            // Assert
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendMessageBackToFetchPrnQueue_ShouldSendRetryMessage()
        {
            // Arrange
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(fixture.Create<Evidence>())),
                messageId: fixture.Create<string>(),
                correlationId: fixture.Create<string>(),
                contentType: "application/json",
                subject: "subject",
                to: "receiver"
            );

            var serviceBusMessage = new ServiceBusMessage(receivedMessage.Body)
            {
                ContentType = receivedMessage.ContentType,
                MessageId = receivedMessage.MessageId,
                CorrelationId = receivedMessage.CorrelationId,
                Subject = receivedMessage.Subject,
                To = receivedMessage.To
            };

            foreach (var property in receivedMessage.ApplicationProperties)
            {
                serviceBusMessage.ApplicationProperties.Add(property.Key, property.Value);
            }

            _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default)).Returns(Task.CompletedTask);
            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

            
            // Act
            await _serviceBusProvider.SendMessageBackToFetchPrnQueue(receivedMessage);

            // Assert
            _serviceBusSenderMock.Verify(sender => sender.SendMessageAsync(It.Is<ServiceBusMessage>(msg =>
                msg.Body.ToString() == serviceBusMessage.Body.ToString() &&
                msg.ContentType == serviceBusMessage.ContentType &&
                msg.MessageId == serviceBusMessage.MessageId &&
                msg.CorrelationId == serviceBusMessage.CorrelationId &&
                msg.Subject == serviceBusMessage.Subject &&
                msg.To == serviceBusMessage.To &&
                msg.ApplicationProperties.SequenceEqual(serviceBusMessage.ApplicationProperties)), default), Times.Once);
        }

        [Fact]
        public async Task SendMessageBackToFetchPrnQueue_Failure_LogsError()
        {
            // Arrange
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(fixture.Create<Evidence>())),
                messageId: fixture.Create<string>(),
                correlationId: fixture.Create<string>(),
                contentType: "application/json",
                subject: "subject",
                to: "receiver"
            );

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("ServiceBus error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendMessageBackToFetchPrnQueue(receivedMessage));

            // Assert
            _loggerMock.VerifyLog(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendMessageToErrorQueue_Success()
        {
            // Arrange
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(fixture.Create<Evidence>())),
                messageId: fixture.Create<string>(),
                correlationId: fixture.Create<string>(),
                contentType: "application/json",
                subject: "subject",
                to: "receiver"
            );

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

            // Act
            await _serviceBusProvider.SendMessageToErrorQueue(receivedMessage);

            // Assert
            _serviceBusSenderMock.Verify(r => r.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _loggerMock.VerifyLog(l => l.LogInformation(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ReceiveFetchedNpwdPrnsFromQueue_NoMessages_ReturnsEmptyList()
        {
            // Arrange
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceBusReceivedMessage>());

            // Act
            var result = await _serviceBusProvider.ReceiveFetchedNpwdPrnsFromQueue();

            // Assert
            Assert.Empty(result);
        }


    [Fact]
        public async Task ReceiveFetchedNpwdPrnsFromQueue_MessagesFound_ReturnsDeserializedList()
        {
            // Arrange
            var npwdPrns = fixture.CreateMany<NpwdPrn>(3).ToList();
            var messages = npwdPrns.Select(prn => ServiceBusModelFactory.ServiceBusReceivedMessage(
                new BinaryData(JsonSerializer.Serialize(npwdPrns)))).ToList();
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            _serviceBusReceiverMock.Setup(receiver => receiver.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _serviceBusProvider.ReceiveFetchedNpwdPrnsFromQueue();

            // Assert
            Assert.Equal(npwdPrns.Count, result.Count());
        }

        [Fact]
        public async Task ReceiveFetchedNpwdPrnsFromQueue_ExceptionThrown_LogsErrorAndRethrows()
        {
            // Arrange
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service bus connection error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.ReceiveFetchedNpwdPrnsFromQueue());
            Assert.Equal("Service bus connection error", exception.Message);
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Failed to receive messages from queue with exception"))), Times.Once);
        }

        [Fact]
        public async Task SendDeltaSyncExecutionToQueue_ExistingMessage_LogsUpdateAndSendsMessage()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };
            var existingMessage = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

            // Mocking ReceiveDeltaSyncExecutionFromQueue to return an existing message
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(JsonSerializer.Serialize(existingMessage)));
            
            _serviceBusReceiverMock.Setup(receiver => receiver
                .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceBusReceivedMessage>{message});

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

            // Act
            await _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution);

            // Assert: Ensure that SendMessageAsync was called with the correct message
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("Updated existing message for SyncType"))), Times.Once);

        }

        [Fact]
        public async Task SendDeltaSyncExecutionToQueue_NoExistingMessage_LogsCreateAndSendsMessage()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

            // Mocking ReceiveDeltaSyncExecutionFromQueue to return null (no existing message)
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

            _serviceBusReceiverMock.Setup(receiver => receiver
                    .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ServiceBusReceivedMessage>)null);

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            // Act
            await _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution);

            // Assert: Ensure that SendMessageAsync was called with the correct message
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("Created new message for SyncType"))), Times.Once);
        }

        [Fact]
        public async Task SendDeltaSyncExecutionToQueue_ThrowsException_LogsError()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

            // Mock ReceiveDeltaSyncExecutionFromQueue to return a valid response
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

            _serviceBusReceiverMock.Setup(receiver => receiver
                    .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<ServiceBusReceivedMessage>)null);

            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("error"));

            // Act & Assert: Ensure that an exception is thrown and error is logged
            await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution));
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("SendDeltaSyncExecutionToQueue failed to add message on Queue with exception"))), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_NoMessages_ReturnsNull()
        {
            // Arrange
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ServiceBusReceivedMessage>().ToList());

            // Act
            var result = await _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

            // Assert
            Assert.Null(result);
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("No messages received from the queue"))), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_MessageFound_DeserializedAndCompleted()
        {
            // Arrange
            var deltaSyncExecution = new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow
            };
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(JsonSerializer.Serialize(deltaSyncExecution)));

            // Mock ReceiveDeltaSyncExecutionFromQueue to return a valid response
            _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

            _serviceBusReceiverMock.Setup(receiver => receiver
                    .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message });


            _serviceBusReceiverMock.Setup(receiver =>
                    receiver.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deltaSyncExecution.SyncType, result.SyncType);
            _serviceBusReceiverMock.Verify(
                receiver => receiver.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_DeserializationFails_LogsErrorAndAbandonsMessage()
        {
            // Arrange
            var invalidMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(JsonSerializer.Serialize("invalid message")));

            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([invalidMessage]);

            _serviceBusReceiverMock.Setup(receiver =>
                    receiver.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                        It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

            // Assert
            Assert.Null(result);
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Error deserializing message"))), Times.Once);
            _serviceBusReceiverMock.Verify(
                receiver => receiver.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                    It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReceiveDeltaSyncExecutionFromQueue_ExceptionThrown_LogsErrorAndRethrows()
        {
            // Arrange
            _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service bus connection error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers));
            Assert.Equal("Service bus connection error", exception.Message);
            _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("ReceiveDeltaSyncExecutionFromQueue failed with exception"))), Times.Once);
        }
    }

}
