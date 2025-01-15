using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EprPrnIntegration.Common.Client;
using Microsoft.Azure.Functions.Worker;
using AutoFixture;
using FluentValidation;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Options;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models.Queues;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Api.Models;
using System.Text.Json;
using EprPrnIntegration.Common.Constants;
using FluentAssertions;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

namespace EprPrnIntegration.Api.UnitTests
{
    public class FetchNpwdIssuedPrnsFunctionTests
    {
        private readonly Fixture _fixture;
        private readonly Mock<ILogger<FetchNpwdIssuedPrnsFunction>> _mockLogger;
        private readonly Mock<INpwdClient> _mockNpwdClient;
        private readonly Mock<IServiceBusProvider> _mockServiceBusProvider;
        private Mock<IOptions<FeatureManagementConfiguration>> _mockFeatureConfig;

        private readonly FetchNpwdIssuedPrnsFunction _function;
        private readonly Mock<IOrganisationService> _mockOrganisationService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IValidator<NpwdPrn>> _mockValidator;
        private readonly Mock<IPrnService> _mockPrnService;
        private readonly Mock<IUtilities> _mockPrnUtilities;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public FetchNpwdIssuedPrnsFunctionTests()
        {
            _fixture = new Fixture();
            _mockLogger = new Mock<ILogger<FetchNpwdIssuedPrnsFunction>>();
            _mockNpwdClient = new Mock<INpwdClient>();
            _mockServiceBusProvider = new Mock<IServiceBusProvider>();
            _mockFeatureConfig = new Mock<IOptions<FeatureManagementConfiguration>>();
            _mockOrganisationService = new Mock<IOrganisationService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockValidator = new Mock<IValidator<NpwdPrn>>();
            _mockPrnService = new Mock<IPrnService>();
            _mockPrnUtilities = new Mock<IUtilities>();
            _mockConfiguration = new Mock<IConfiguration>();

            _function = new FetchNpwdIssuedPrnsFunction(
                _mockLogger.Object,
                _mockNpwdClient.Object,
                _mockServiceBusProvider.Object,
                _mockEmailService.Object,
                _mockOrganisationService.Object,
                _mockPrnService.Object,
                _mockValidator.Object,
                _mockFeatureConfig.Object,
                _mockPrnUtilities.Object,
                _mockConfiguration.Object);

            var config = new FeatureManagementConfiguration
            {
                RunIntegration = true
            };
            _mockFeatureConfig.Setup(c => c.Value).Returns(config);
            _mockConfiguration
            .Setup(config => config["DefaultLastRunDate"])
            .Returns("2024-12-01");
        }

        [Fact]
        public async Task Run_Ends_When_Feature_Flag_Is_False()
        {
            // Arrange
            var config = new FeatureManagementConfiguration
            {
                RunIntegration = false
            };
            _mockFeatureConfig.Setup(c => c.Value).Returns(config);

            // Act
            await _function.Run(new TimerInfo());

            // Assert
            _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("FetchNpwdIssuedPrnsFunction function is disabled by feature flag"))));
        }

        [Fact]
        public async Task Run_FetchesPrnsAndPushesToQueue_Successfully()
        {
            // Arrange
            var npwdIssuedPrns = _fixture.CreateMany<NpwdPrn>().ToList();
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync(npwdIssuedPrns);

            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
            .Returns(Task.CompletedTask);

            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = DateTime.Parse("2022-01-01T00:00:00Z"), SyncType = NpwdDeltaSyncType.UpdatePrns };
            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);
            _mockPrnUtilities.Setup(utils => utils.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            // Act
            await _function.Run(new TimerInfo());

            // Assert
            _mockNpwdClient.Verify(client => client.GetIssuedPrns(It.IsAny<string>()), Times.Once);
            _mockServiceBusProvider.Verify(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()), Times.Once);
            _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("function started"))));
            _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("Prns Pushed into Message"))));
            _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("function Completed"))));
        }

        [Fact]
        public async Task Run_NoPrnsFetched_LogsWarning()
        {
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync(new List<NpwdPrn>());

            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
                                   .Returns(Task.CompletedTask);

            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = DateTime.Parse("2022-01-01T00:00:00Z"), SyncType = NpwdDeltaSyncType.UpdatePrns };
            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);
            _mockPrnUtilities.Setup(utils => utils.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            // Act
            await _function.Run(new TimerInfo());

            // Assert
            _mockNpwdClient.Verify(client => client.GetIssuedPrns(It.IsAny<string>()), Times.Once);
            _mockLogger.VerifyLog(x => x.LogWarning(It.Is<string>(s => s.Contains("No Prns Exists"))));
        }

        [Fact]
        public async Task Run_FetchPrnsThrowsHttpException_LogsErrorAndThrows()
        {
            var exception = new HttpRequestException("Error fetching PRNs");
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ThrowsAsync(exception);

            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = DateTime.Parse("2022-01-01T00:00:00Z"), SyncType = NpwdDeltaSyncType.UpdatePrns };
            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);
            _mockPrnUtilities.Setup(utils => utils.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));
            _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Failed Get Prns from npwd"))), Times.Once);
            Assert.Equal("Error fetching PRNs", ex.Message);
            _mockEmailService.Verify(email => email.SendErrorEmailToNpwd(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(System.Net.HttpStatusCode.InternalServerError)]
        [InlineData(System.Net.HttpStatusCode.RequestTimeout)]
        [InlineData(System.Net.HttpStatusCode.GatewayTimeout)]
        public async Task Run_FetchPrnsSend_EmailToNpwd_When_ServerSide_Error_Occurs(System.Net.HttpStatusCode statusCode)
        {
            var exception = new HttpRequestException("Error fetching PRNs", null, statusCode);
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ThrowsAsync(exception);

            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = DateTime.Parse("2022-01-01T00:00:00Z"), SyncType = NpwdDeltaSyncType.UpdatePrns };
            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);
            _mockPrnUtilities.Setup(utils => utils.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));
            _mockEmailService.Verify(email => email.SendErrorEmailToNpwd(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Run_FetchPrnsThrowsOtherException_LogsErrorAndThrows()
        {
            var exception = new InvalidCastException("Invalid cast");
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidCastException>(() => _function.Run(new TimerInfo()));
            _mockLogger.VerifyLog(logger => logger.LogError(It.IsAny<InvalidCastException>(),
                It.Is<string>(s => s.Contains("Failed Get Prns method for filter"))), Times.Once);
            Assert.Equal("Invalid cast", ex.Message);
        }

        [Fact]
        public async Task Run_PushPrnsToQueueThrowsException_LogsErrorAndThrows()
        {
            // Arrange
            var npwdIssuedPrns = _fixture.CreateMany<NpwdPrn>().ToList();
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync(npwdIssuedPrns);

            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = DateTime.Parse("2022-01-01T00:00:00Z"), SyncType = NpwdDeltaSyncType.UpdatePrns };
            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);
            _mockPrnUtilities.Setup(utils => utils.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            var exception = new Exception("Error pushing to queue");
            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
                                   .ThrowsAsync(exception);

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _function.Run(new TimerInfo()));
            _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Failed pushing issued prns in message queue"))), Times.Once);
            Assert.Equal("Error pushing to queue", ex.Message);
        }

        [Fact]
        public async Task Run_CatchesExceptionForProcess_LogsErrorAndThrows()
        {
            // Arrange
            var npwdIssuedPrns = _fixture.CreateMany<NpwdPrn>().ToList();
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync(npwdIssuedPrns);

            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
                                   .Returns(Task.CompletedTask);

            _mockServiceBusProvider.Setup(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                       .Throws(new HttpRequestException());

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));
            _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Failed fetching prns from the queue"))), Times.Once);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_NoMessagesInQueue_LogsInformationAndExits()
        {
            // Arrange
            _mockServiceBusProvider.Setup(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage>());

            // Act
            await _function.ProcessIssuedPrnsAsync();

            // Assert
            _mockLogger.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("No messages found in the queue"))), Times.Once);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_ValidMessage_ProcessesAndSendsEmail()
        {
            // Arrange
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(JsonSerializer.Serialize(npwdPrn)),
                messageId: "message-id"
            );

            _mockServiceBusProvider.SetupSequence(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message })
                .ReturnsAsync([]);

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var producerEmailsTask = Task.FromResult(_fixture.CreateMany<PersonEmail>().ToList());
            _mockOrganisationService.Setup(service => service.GetPersonEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(producerEmailsTask);

            // Act
            await _function.ProcessIssuedPrnsAsync();

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Successfully processed and sent emails")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);

            _mockPrnUtilities.Verify(provider => provider.AddCustomEvent(It.Is<string>(s => s == CustomEvents.NpwdPrnValidationError),
                It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_InvalidMessage_SendsToErrorQueue()
        {
            // Arrange
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(JsonSerializer.Serialize(npwdPrn)),
                messageId: "message-id"
            );

            _mockServiceBusProvider.SetupSequence(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message })
                .ReturnsAsync([]);

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new FluentValidation.Results.ValidationResult { Errors = { new FluentValidation.Results.ValidationFailure("Error", "Validation failed") } });

            _mockServiceBusProvider.Setup(provider => provider.SendMessageToErrorQueue(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _function.ProcessIssuedPrnsAsync();

            // Assert
            _mockServiceBusProvider.Verify(provider => provider.SendMessageToErrorQueue(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>()), Times.Once);
            _mockLogger.VerifyLog(logger => logger.LogWarning(It.Is<string>(s => s.Contains("Validation failed for message Id"))), Times.Once);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_InvalidMessage_AddsCustomEvent()
        {
            // Arrange
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(JsonSerializer.Serialize(npwdPrn)),
                messageId: "message-id"
            );

            _mockServiceBusProvider.SetupSequence(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message })
                .ReturnsAsync([]);

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new FluentValidation.Results.ValidationResult { Errors = { new FluentValidation.Results.ValidationFailure("Error", "Validation failed") } });

            _mockServiceBusProvider.Setup(provider => provider.SendMessageToErrorQueue(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _function.ProcessIssuedPrnsAsync();

            // Assert
            _mockPrnUtilities.Verify(provider => provider.AddCustomEvent(It.Is<string>(s => s == CustomEvents.NpwdPrnValidationError),
                It.Is<Dictionary<string, string>>(
                data => data["Error Comments"] == "Validation failed"
                )), Times.Once);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_InvalidMessage_AddsCustomEvent_WithMultipleErrorComments()
        {
            // Arrange
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(JsonSerializer.Serialize(npwdPrn)),
                messageId: "message-id"
            );

            _mockServiceBusProvider.SetupSequence(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message })
                .ReturnsAsync([]);

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new FluentValidation.Results.ValidationResult {
                                Errors = { new FluentValidation.Results.ValidationFailure("Error", "Validation failed 1"), new FluentValidation.Results.ValidationFailure("Error", "Validation failed 2") } 
                            });

            _mockServiceBusProvider.Setup(provider => provider.SendMessageToErrorQueue(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _function.ProcessIssuedPrnsAsync();

            // Assert
            _mockPrnUtilities.Verify(provider => provider.AddCustomEvent(It.Is<string>(s => s == CustomEvents.NpwdPrnValidationError),
                It.Is<Dictionary<string, string>>(
                data => data["Error Comments"] == "Validation failed 1 | Validation failed 2"
                )), Times.Once);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_ExceptionDuringProcessing_AddsMessageBackToErrorQueue()
        {
            // Arrange
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(JsonSerializer.Serialize(npwdPrn)),
                messageId: "message-id"
            );

            _mockServiceBusProvider.SetupSequence(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message })
                .ReturnsAsync([]);
            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            _mockPrnService.Setup(service => service.SavePrn(It.IsAny<SavePrnDetailsRequest>()))
                .ThrowsAsync(new Exception("Error saving PRN"));

            _mockServiceBusProvider.Setup(provider => provider.SendMessageBackToFetchPrnQueue(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _function.ProcessIssuedPrnsAsync();

            // Assert
            _mockServiceBusProvider.Verify(provider => provider.SendMessageToErrorQueue(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>()), Times.Once);
            _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Error processing message Id"))), Times.Once);
        }

        [Fact]
        public async Task ProcessIssuedPrnsAsync_ExceptionDuringValidation_LogsError()
        {
            // Arrange
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(JsonSerializer.Serialize(npwdPrn)),
                messageId: "message-id"
            );

            _mockServiceBusProvider.SetupSequence(provider => provider.ReceiveFetchedNpwdPrnsFromQueue())
                .ReturnsAsync(new List<ServiceBusReceivedMessage> { message })
                .ReturnsAsync([]);

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Some unexpected problem")); ;

            // Act and Assert
            bool rethrowsException = false;
            try
            {
                await _function.ProcessIssuedPrnsAsync();
                _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.StartsWith("Unexpected Error processing message Id"))), Times.Once);
            }
            catch (Exception) {
                rethrowsException = true;
            }

            rethrowsException.Should().BeTrue();
        }

        [Fact]
        public async Task Run_UsesDefaultFilterIfLastSyncDateIsBeforeDefaultLastRunDate()
        {
            // Arrange
            var npwdIssuedPrns = _fixture.CreateMany<NpwdPrn>().ToList();
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync(npwdIssuedPrns);

            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
                                   .Returns(Task.CompletedTask);

            var lastSyncDate = new DateTime(2024, 10, 01, 00, 00, 00, DateTimeKind.Utc); // Before default last run date
            var defaultLastRunDate = new DateTime(2024, 11, 01, 00, 00, 00, DateTimeKind.Utc);
            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = lastSyncDate, SyncType = NpwdDeltaSyncType.FetchNpwdIssuedPrns };

            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
                             .ReturnsAsync(deltaSyncExecution);

            _mockConfiguration.Setup(config => config["DefaultLastRunDate"])
                              .Returns(defaultLastRunDate.ToString("O"));

            _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<NpwdPrn>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            // Act
            await _function.Run(new TimerInfo());

            // Assert
            var expectedFilter = "(EvidenceStatusCode eq 'EV-CANCEL' or EvidenceStatusCode eq 'EV-AWACCEP' or EvidenceStatusCode eq 'EV-AWACCEP-EPR')";
            _mockNpwdClient.Verify(client => client.GetIssuedPrns(It.Is<string>(filter => filter == expectedFilter)), Times.Once);
            _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("function started"))));
            _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("Prns Pushed into Message"))));
        }

        [Fact]
        public async Task Run_AddDateFilterIfLastSyncDateIsPresentAndNotDeafult()
        {
            var passedFilter = "";
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>())).ReturnsAsync([])
                .Callback<string>(f => passedFilter = f);

            var defaultLastRunDate = DateTime.UtcNow.AddDays(-10);
            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = defaultLastRunDate.AddDays(1), SyncType = NpwdDeltaSyncType.FetchNpwdIssuedPrns };

            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);

            _mockConfiguration.Setup(config => config["DefaultLastRunDate"]).Returns(defaultLastRunDate.ToString("O"));

            // Act
            await _function.Run(new TimerInfo());

            passedFilter.Should().Contain($"and ((StatusDate ge {deltaSyncExecution.LastSyncDateTime.ToUniversalTime():O} and StatusDate lt");
            passedFilter.Should().Contain($"or (ModifiedOn ge {deltaSyncExecution.LastSyncDateTime.ToUniversalTime():O} and ModifiedOn lt");
        }

        [Fact]
        public async Task Run_AddCustomEventForFetchedPrns()
        {
            // Arrange
            var npwdIssuedPrns = _fixture.CreateMany<NpwdPrn>().ToList();
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync(npwdIssuedPrns);

            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
            .Returns(Task.CompletedTask);

            var deltaSyncExecution = new DeltaSyncExecution { LastSyncDateTime = DateTime.Parse("2022-01-01T00:00:00Z"), SyncType = NpwdDeltaSyncType.UpdatePrns };
            _mockPrnUtilities.Setup(utils => utils.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>())).ReturnsAsync(deltaSyncExecution);
            _mockPrnUtilities.Setup(utils => utils.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            // Act
            await _function.Run(new TimerInfo());

            _mockPrnUtilities.Verify(provider => provider.AddCustomEvent(It.Is<string>(s => s == CustomEvents.IssuedPrn),
                It.IsAny<Dictionary<string, string>>()), Times.Exactly(3));
        }

        [Fact]
        public async Task Run_AddCustomEventForFetchedPrnsLogsDefaultValuesIfValueDoesntExists()
        {
            // Arrange
            var npwdIssuedPrn = _fixture.Create<NpwdPrn>();

            npwdIssuedPrn.EvidenceNo = npwdIssuedPrn.EvidenceStatusCode = npwdIssuedPrn.IssuedToOrgName = null;
            _mockNpwdClient.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
                           .ReturnsAsync([npwdIssuedPrn]);

            _mockServiceBusProvider.Setup(provider => provider.SendFetchedNpwdPrnsToQueue(It.IsAny<List<NpwdPrn>>()))
            .Returns(Task.CompletedTask);

            // Act
            await _function.Run(new TimerInfo());

            _mockPrnUtilities.Verify(provider => provider.AddCustomEvent(It.Is<string>(s => s == CustomEvents.IssuedPrn),
                It.Is<Dictionary<string, string>>(
                    data => data["PRN Number"] == "No PRN Number" &&
                    data["PRN Number"] == "No PRN Number" &&
                    data["Organisation Name"] == "Blank Organisation Name"

                    )), Times.Once);
        }

    }
}
