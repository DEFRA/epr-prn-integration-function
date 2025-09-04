using System.Text.Json;
using AutoFixture;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Services;

public class ServiceBusProviderTests
{
    private readonly Mock<ILogger<ServiceBusProvider>> _loggerMock;
    private readonly Mock<ServiceBusClient> _serviceBusClientMock;
    private readonly Mock<ServiceBusSender> _serviceBusSenderMock;
    private readonly ServiceBusProvider _serviceBusProvider;
    private readonly Mock<ServiceBusReceiver> _serviceBusReceiverMock;
    private readonly Fixture _fixture;

    public ServiceBusProviderTests()
    {
        _fixture = new Fixture();
        _loggerMock = new Mock<ILogger<ServiceBusProvider>>();
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        Mock<IOptions<ServiceBusConfiguration>> configMock = new();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();
        _serviceBusReceiverMock = new Mock<ServiceBusReceiver>();

        var config = new ServiceBusConfiguration
        {
            FetchPrnQueueName = "test-queue-1",
            ErrorPrnQueue = "test-queue-2",
            UpdateProducerDeltaSyncQueueName = "UpdateProducerDeltaSyncQueueName",
            UpdatePrnDeltaSyncQueueName = "UpdatePrnDeltaSyncQueueName",
            FetchPrnDeltaSyncQueueName = "FetchPrnDeltaSyncQueueName"
        };

        configMock.Setup(c => c.Value).Returns(config);

        // Set up ServiceBusClient to return the mock receiver
        _serviceBusClientMock
            .Setup(client => client.CreateReceiver(It.IsAny<string>()))
            .Returns(_serviceBusReceiverMock.Object);

        _serviceBusProvider = new ServiceBusProvider(
            _loggerMock.Object,
            _serviceBusClientMock.Object,
            configMock.Object
        );
    }

    [Fact]
    public async Task SendFetchedNpwdPrnsToQueue_SuccessfullyPushMessage()
    {
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(500, []);

        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        var npwdPrns = _fixture.CreateMany<NpwdPrn>().ToList();

        await _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns);

        _serviceBusSenderMock.Verify(sender => sender.CreateMessageBatchAsync(default), Times.Once);
        _serviceBusSenderMock.Verify(sender => sender.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), default), Times.Once);
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.VerifyLog(l => l.LogInformation(It.Is<string>(msg => msg.Contains("SendFetchedNpwdPrnsToQueue"))), Times.Exactly(2));

    }

    [Fact]
    public async Task SendFetchedPrnsToQueue_SendBatchAndCreateNewAndAddMessage()
    {
        // Arrange
        var npwdPrns = _fixture.CreateMany<NpwdPrn>(10).ToList();
        int messageCountThreshold = 6;
        List<ServiceBusMessage> messageList1 = [];
        List<ServiceBusMessage> messageList2 = [];
        ServiceBusMessageBatch messageBatch1 = ServiceBusModelFactory.ServiceBusMessageBatch(
            batchSizeBytes: 10 * 1024 * 1024,
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
    public async Task SendFetchedPrnsToQueue_ShouldThrowError_WHenClientFails()
    {
        // Arrange
        var npwdPrns = _fixture.CreateMany<NpwdPrn>().ToList();

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("error"));
        // Act

        await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns));

        // Assert
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.VerifyLog(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendFetchedNpwdPrnsToQueue_MessageTooLarge_Warns()
    {
        // Arrange
        var npwdPrns = _fixture.CreateMany<NpwdPrn>(10).ToList();
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
        var npwdPrns = _fixture.CreateMany<NpwdPrn>().ToList();

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("ServiceBus error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns));

        // Assert
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.VerifyLog(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendDeltaSyncExecutionToQueue_NoExistingMessage_LogsCreateAndSendsMessage()
    {
        // Arrange
        var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };
        var executionMessage = JsonSerializer.Serialize(deltaSyncExecution);

        // Mocking GetDeltaSyncExecutionFromQueue to return null (no existing message)
        _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>())).Returns(_serviceBusReceiverMock.Object);

        _serviceBusReceiverMock.Setup(receiver => receiver
                .ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ServiceBusReceivedMessage>)null!);

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        // Act
        await _serviceBusProvider.SendDeltaSyncExecutionToQueue(deltaSyncExecution);

        // Assert: Ensure that SendMessageAsync was called with the correct message
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("SendDeltaSyncExecutionToQueue - A message has been published to the queue"))), Times.Once);
    }

    [Fact]
    public async Task SendDeltaSyncExecutionToQueue_ThrowsException_LogsError()
    {
        // Arrange
        var deltaSyncExecution = new DeltaSyncExecution { SyncType = NpwdDeltaSyncType.UpdatedProducers };

        // Mock GetDeltaSyncExecutionFromQueue to return a valid response
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
        _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceBusReceivedMessage?)null);

        // Act
        var result = await _serviceBusProvider.GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

        // Assert
        Assert.Null(result);
        _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("No message received from the queue"))), Times.Once);
    }

    [Fact]
    public async Task ReceiveDeltaSyncExecutionFromQueue_Returns_LatestMessageBasedOnSequence()
    {
        // Arrange
        var sync1 = new DeltaSyncExecution() { LastSyncDateTime = DateTimeHelper.Parse("2024-10-08"), SyncType = NpwdDeltaSyncType.FetchNpwdIssuedPrns };
        var sync2 = new DeltaSyncExecution() { LastSyncDateTime = DateTimeHelper.Parse("2024-10-09"), SyncType = NpwdDeltaSyncType.FetchNpwdIssuedPrns }; 
        var sync3 = new DeltaSyncExecution() { LastSyncDateTime = DateTimeHelper.Parse("2024-10-10"), SyncType = NpwdDeltaSyncType.FetchNpwdIssuedPrns };

        var message1 = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(sync1), sequenceNumber: 1 );
        var message2 = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(sync2), sequenceNumber: 2);
        var latestMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(sync3), sequenceNumber: 3);

        _serviceBusReceiverMock
            .Setup(r => r.ReceiveMessagesAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([message1, message2, latestMessage]);

        int completeMessageCalls = 0;
        _serviceBusReceiverMock
            .Setup(r => r.CompleteMessageAsync(
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => completeMessageCalls++)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _serviceBusProvider.GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.FetchNpwdIssuedPrns);

        result.Should().BeEquivalentTo(sync3);

        _serviceBusReceiverMock.Verify(r => r.CompleteMessageAsync(
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<CancellationToken>()), Times.Exactly(completeMessageCalls));

        _serviceBusReceiverMock.Verify(r => r.DisposeAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ReceiveDeltaSyncExecutionFromQueue_DeserializationFails_LogsErrorAndAbandonsMessage()
    {
        // Arrange
        var invalidMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(JsonSerializer.Serialize("invalid message")));

        _serviceBusReceiverMock
                    .Setup(r => r.ReceiveMessagesAsync(
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync([invalidMessage]);

        _serviceBusReceiverMock.Setup(receiver =>
                receiver.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                    It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _serviceBusProvider.GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers);

        // Assert
        Assert.Null(result);
        _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Failed to deserialize message body:"))), Times.Once);
    }

    [Fact]
    public async Task ReceiveDeltaSyncExecutionFromQueue_ExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(),default))
            .ThrowsAsync(new Exception("Service bus connection error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _serviceBusProvider.GetDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers));
        Assert.Equal("Service bus connection error", exception.Message);
        _loggerMock.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("GetDeltaSyncExecutionFromQueue failed with exception"))), Times.Once);
    }

    [Fact]
    public void GetDeltaSyncQueueName_ReturnsCorrectQueueName()
    {
        // Arrange
        var expectedUpdatedProducersQueue = "UpdateProducerDeltaSyncQueueName";
        var expectedUpdatePrnsQueue = "UpdatePrnDeltaSyncQueueName";
        var expectedFetchNpwdIssuedPrnsQueue = "FetchPrnDeltaSyncQueueName";

        // Act
        var updatedProducersQueue = _serviceBusProvider.GetType()
            .GetMethod("GetDeltaSyncQueueName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_serviceBusProvider, [NpwdDeltaSyncType.UpdatedProducers]);
        var updatePrnsQueue = _serviceBusProvider.GetType()
            .GetMethod("GetDeltaSyncQueueName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_serviceBusProvider, [NpwdDeltaSyncType.UpdatePrns]);
        var fetchNpwdIssuedPrnsQueue = _serviceBusProvider.GetType()
            .GetMethod("GetDeltaSyncQueueName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_serviceBusProvider, [NpwdDeltaSyncType.FetchNpwdIssuedPrns]);

        // Assert
        updatedProducersQueue.Should().Be(expectedUpdatedProducersQueue);
        updatePrnsQueue.Should().Be(expectedUpdatePrnsQueue);
        fetchNpwdIssuedPrnsQueue.Should().Be(expectedFetchNpwdIssuedPrnsQueue);
    }

    [Fact]
    public async Task SendMessageToErrorQueue_Success()
    {
        // Arrange
        var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(_fixture.Create<Common.Models.Npwd.Evidence>())),
            messageId: _fixture.Create<string>(),
            correlationId: _fixture.Create<string>(),
            contentType: "application/json",
            subject: "subject",
            to: "receiver"
        );

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        // Act
        await _serviceBusProvider.SendMessageToErrorQueue(receivedMessage, "EvidenceNo");

        // Assert
        _serviceBusSenderMock.Verify(r => r.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.VerifyLog(l => l.LogInformation(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageToErrorQueue_SuccessfullySendsMessage()
    {
        // Arrange
        var evidenceNo = "12345";
        var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData("Test message body"),
            messageId: "message-id-1",
            correlationId: "correlation-id-1",
            contentType: "application/json",
            subject: "Test Subject",
            to: "Test Receiver"
        );

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>()))
            .Returns(_serviceBusSenderMock.Object);

        // Act
        await _serviceBusProvider.SendMessageToErrorQueue(receivedMessage, evidenceNo);

        // Assert
        _serviceBusSenderMock.Verify(sender => sender.SendMessageAsync(It.Is<ServiceBusMessage>(msg =>
            msg.Body.ToString() == receivedMessage.Body.ToString() &&
            msg.MessageId == receivedMessage.MessageId &&
            msg.CorrelationId == receivedMessage.CorrelationId &&
            msg.ContentType == receivedMessage.ContentType &&
            msg.Subject == receivedMessage.Subject &&
            msg.To == receivedMessage.To &&
            msg.ApplicationProperties.SequenceEqual(receivedMessage.ApplicationProperties)), It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.VerifyLog(logger => logger.LogInformation(
            "Message with EvidenceNo: {EvidenceNo} sent to error queue.", evidenceNo), Times.Once);
    }

    [Fact]
    public async Task SendMessageToErrorQueue_LogsErrorOnException()
    {
        // Arrange
        var evidenceNo = "54321";
        var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData("Test message body"),
            messageId: "message-id-2"
        );

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>()))
            .Returns(_serviceBusSenderMock.Object);

        _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("ServiceBus exception"));

        // Act
        await _serviceBusProvider.SendMessageToErrorQueue(receivedMessage, evidenceNo);

        // Assert
        _loggerMock.VerifyLog(logger => logger.LogError(
            "Failed to send message to error queue with exception: {ExceptionMessage}",
            "ServiceBus exception"), Times.Once);
    }

    [Fact]
    public async Task ProcessFetchedPrns_ShouldNotCallHandlerAndBreakLoop_WhenNoMessagesFoundInQueue()
    {
        int handlerCallCount = 0;
        var handler = (ServiceBusReceivedMessage message) => { handlerCallCount++; return Task.FromResult<string?>(null); };

        _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
        _serviceBusReceiverMock.Setup(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        await _serviceBusProvider.ProcessFetchedPrns(handler);
        _loggerMock.VerifyLog(logger => logger.LogInformation("No messages found in the queue. Exiting the processing loop."), Times.Once);
        handlerCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessFetchedPrns_ShouldCallHandlerAndBreakLoop_WhenMessagesFoundInQueue()
    {
        int handlerCallCount = 0;
        var handler = (ServiceBusReceivedMessage message) => { handlerCallCount++; return Task.FromResult<string?>(null); };

        var npwdPrns = _fixture.CreateMany<NpwdPrn>(3).ToList();
        var messages = npwdPrns.Select(prn => ServiceBusModelFactory.ServiceBusReceivedMessage(
            new BinaryData(JsonSerializer.Serialize(npwdPrns)))).ToList();

        _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
        _serviceBusReceiverMock.SetupSequence(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages)
            .ReturnsAsync([]);

        await _serviceBusProvider.ProcessFetchedPrns(handler);
        _loggerMock.VerifyLog(logger => logger.LogInformation("No messages found in the queue. Exiting the processing loop."), Times.Once);

        handlerCallCount.Should().Be(3);

        _serviceBusClientMock.Verify(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()), Times.Once);
        _serviceBusReceiverMock.Verify(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessFetchedPrns_ShoulReturnListOfReturnsOfHandler()
    {
        int handlerCallCount = 0;
        var handler = (ServiceBusReceivedMessage message) =>
        {
            handlerCallCount++;
            if (handlerCallCount % 2 == 0)
                return Task.FromResult<string?>(message.Body.ToString());
            else
                return Task.FromResult<string?>(null);
        };

        var npwdPrns = _fixture.CreateMany<NpwdPrn>(3).ToList();
        var messages = npwdPrns.Select(prn => ServiceBusModelFactory.ServiceBusReceivedMessage(
            new BinaryData(JsonSerializer.Serialize(npwdPrns)))).ToList();

        _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
        _serviceBusReceiverMock.SetupSequence(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages)
            .ReturnsAsync([]);

        var response = await _serviceBusProvider.ProcessFetchedPrns(handler);

        _loggerMock.VerifyLog(logger => logger.LogInformation("No messages found in the queue. Exiting the processing loop."), Times.Once);

        handlerCallCount.Should().Be(3);

        _serviceBusClientMock.Verify(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()), Times.Once);
        _serviceBusReceiverMock.Verify(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessFetchedPrns_ShouldLogError_WhenMessageHandlerThrowsException()
    {
        var handler = (ServiceBusReceivedMessage message) => { return Task.FromException<string?>(new NotImplementedException()); };

        var npwdPrns = _fixture.CreateMany<NpwdPrn>(3).ToList();
        var messages = npwdPrns.Select(prn => ServiceBusModelFactory.ServiceBusReceivedMessage(
            new BinaryData(JsonSerializer.Serialize(npwdPrns)))).ToList();

        _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiverMock.Object);
        _serviceBusReceiverMock.SetupSequence(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages)
            .ReturnsAsync([]);

        await _serviceBusProvider.ProcessFetchedPrns(handler);

        _loggerMock.VerifyLog(logger => logger.LogInformation("No messages found in the queue. Exiting the processing loop."), Times.Once);

        _serviceBusClientMock.Verify(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()), Times.Once);
        _serviceBusReceiverMock.Verify(receiver => receiver.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process message with id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(3));

    }

    [Fact]
    public async Task ProcessFetchedPrns_ShouldLogError_WhenReceiverFails()
    {
        var handler = (ServiceBusReceivedMessage message) => { return Task.FromResult<string?>(null); };
        // Arrange
        _serviceBusClientMock.Setup(client => client.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()))
            .Throws(new Exception("Receiver exception"));

        await _serviceBusProvider.ProcessFetchedPrns(handler);
        _loggerMock.Verify(logger => logger.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to receive messages from queue:")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

    }
}