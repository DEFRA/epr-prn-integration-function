using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using global::EprPrnIntegration.Common.Client;
using global::EprPrnIntegration.Common.Models;
using global::EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace EprPrnIntegration.Api.UnitTests
{
    public class UpdatePrnsFunctionTests
    {
        private readonly Mock<IPrnService> _mockPrnService;
        private readonly Mock<INpwdClient> _mockNpwdClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<UpdatePrnsFunction>> _loggerMock;
        private UpdatePrnsFunction _function;

        public UpdatePrnsFunctionTests()
        {
            _mockPrnService = new Mock<IPrnService>();
            _mockNpwdClient = new Mock<INpwdClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<UpdatePrnsFunction>>();
            _mockConfiguration.Setup(c => c["PrnsContext"]).Returns("https://fat.npwd.org.uk/odata/PRNs/$delta");
            _function = new UpdatePrnsFunction(
                _mockPrnService.Object,
                _mockNpwdClient.Object,
                _loggerMock.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task Run_ShouldUseDefaultStartHour_WhenConfigurationIsInvalid()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["UpdatePrnsStartHour"]).Returns("invalid");
            var mockTimerInfo = new Mock<TimerInfo>();

            // Act
            await _function.Run(null);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid StartHour configuration value")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Using default value of 18(6pm)")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once,
                "Expected log message containing 'Using default value of 18(6pm)'"
            );
        }

        [Fact]
        public async Task Run_ShouldLogWarning_WhenNoUpdatedPrnsRetrieved()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UpdatedPrnsResponseModel>());
            var mockTimerInfo = new Mock<TimerInfo>();

            // Act
            await _function.Run(null);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No updated Prns are retrieved from common database")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task Run_ShouldLogSuccess_WhenPrnListUpdatedSuccessfully()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

            _mockNpwdClient.Setup(c => c.Patch(It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            var mockTimerInfo = new Mock<TimerInfo>();

            // Act
            await _function.Run(null);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Prns list successfully updated in NPWD")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task Run_ShouldLogError_WhenPrnListUpdateFails()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

            _mockNpwdClient.Setup(c => c.Patch(It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Bad Request")
                });

            // Act
            await _function.Run(null);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update Prns list in NPWD")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Status Code: BadRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }


        [Fact]
        public async Task Run_ShouldLogError_WhenPrnServiceThrowsException()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));
            var mockTimerInfo = new Mock<TimerInfo>();

            // Act
            await _function.Run(null);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve data from common backend")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("form time period")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }
}
