﻿using AutoFixture;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
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
            _loggerMock.VerifyLog(l => l.LogInformation(It.IsAny<string>()), Times.Once);

        }

        [Fact]
        public async Task SendApprovedSubmissionsToQueueAsync_MessageTooMany_Warns()
        {
            // Arrange
            var npwdPrns = fixture.CreateMany<NpwdPrn>(10).ToList();
            int messageCountThreshold = 1;
            List<ServiceBusMessage> messageList = [];
            messageList.Add(new ServiceBusMessage());
            ServiceBusMessageBatch messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(
                batchSizeBytes: 500,
                batchMessageStore: messageList,
                batchOptions: new CreateMessageBatchOptions(),
                tryAddCallback: _ => messageList.Count < messageCountThreshold);
            _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
            _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
            // Act
            await _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdPrns);

            // Assert
            _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
            _loggerMock.VerifyLog(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(10));
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
            _loggerMock.VerifyLog(l => l.LogError(It.IsAny<Exception>(),It.IsAny<string>()), Times.Once);
        }
    }
}
