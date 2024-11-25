using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using global::EprPrnIntegration.Common.Client;
using global::EprPrnIntegration.Common.Models;
using global::EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using global::EprPrnIntegration.Api.UnitTests.Helpers;
using System.Net;

namespace EprPrnIntegration.Api.UnitTests
{
    public class UpdatePrnsFunctionTests
    {
        private readonly Mock<IPrnService> _mockPrnService;
        private readonly Mock<INpwdClient> _mockNpwdClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly MockLogger<UpdatePrnsFunction> _mockLogger;
        private UpdatePrnsFunction _function;

        public UpdatePrnsFunctionTests()
        {
            _mockPrnService = new Mock<IPrnService>();
            _mockNpwdClient = new Mock<INpwdClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new MockLogger<UpdatePrnsFunction>();

            _function = new UpdatePrnsFunction(
                _mockPrnService.Object,
                _mockNpwdClient.Object,
                _mockLogger,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task Run_ShouldUseDefaultStartHour_WhenConfigurationIsInvalid()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["UpdatePrnsStartHour"]).Returns("invalid");

            // Act
            await _function.Run(null);

            // Assert
            var logMessage = _mockLogger.LogMessages
                .FirstOrDefault(msg => msg.Contains("Invalid StartHour configuration value"));
            Assert.NotNull(logMessage);
            Assert.Contains("Using default value of 18(6pm)", logMessage);
        }

        [Fact]
        public async Task Run_ShouldLogWarning_WhenNoUpdatedPrnsRetrieved()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UpdatedPrnsResponseModel>());

            // Act
            await _function.Run(null);

            // Assert
            var logMessage = _mockLogger.LogMessages
                .FirstOrDefault(msg => msg.Contains("No updated Prns are retrieved from common database"));
            Assert.NotNull(logMessage);
        }
               

        [Fact]
        public async Task Run_ShouldLogSuccess_WhenPrnListUpdatedSuccessfully()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

            _mockNpwdClient.Setup(c => c.Patch(It.IsAny<List<UpdatedPrnsResponseModel>>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            await _function.Run(null);

            // Assert
            var logMessage = _mockLogger.LogMessages
                .FirstOrDefault(msg => msg.Contains("Prns list successfully updated in NPWD"));
            Assert.NotNull(logMessage);
        }

        [Fact]
        public async Task Run_ShouldLogError_WhenPrnListUpdateFails()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

            _mockNpwdClient.Setup(c => c.Patch(It.IsAny<List<UpdatedPrnsResponseModel>>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            // Act
            await _function.Run(null);

            // Assert
            var logMessage = _mockLogger.LogMessages
                .FirstOrDefault(msg => msg.Contains("Failed to update Prns list in NPWD"));
            Assert.NotNull(logMessage);
            Assert.Contains("Status Code: BadRequest", logMessage);
        }

        [Fact]
        public async Task Run_ShouldLogError_WhenPrnServiceThrowsException()
        {
            // Arrange
            _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            await _function.Run(null);

            // Assert
            var logMessage = _mockLogger.LogMessages
                .FirstOrDefault(msg => msg.Contains("Failed to retrieve data from common backend"));
            Assert.NotNull(logMessage);
        }
    }
}