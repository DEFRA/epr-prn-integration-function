using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notify.Interfaces;
using Notify.Models.Responses;

namespace EprPrnIntegration.Common.UnitTests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<INotificationClient> _mockNotificationClient;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly MessagingConfig _mockMessagingConfig;
        private readonly IOptions<MessagingConfig> _mockOptions;

        public EmailServiceTests()
        {
            _mockNotificationClient = new Mock<INotificationClient>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockMessagingConfig = new MessagingConfig
            {
                NpwdEmail = "test@example.com",
                NpwdEmailTemplateId = "template123"
            };
            _mockOptions = Mock.Of<IOptions<MessagingConfig>>(opt => opt.Value == _mockMessagingConfig);
        }

        private EmailService CreateEmailService() =>
            new EmailService(_mockNotificationClient.Object, _mockOptions, _mockLogger.Object);

        [Fact]
        public void SendEmailToNpwd_ShouldLogInformation_WhenEmailIsSentSuccessfully()
        {
            // Arrange
            var response = new EmailNotificationResponse
            {
                id = "response123"
            };

            _mockNotificationClient
                .Setup(client => client.SendEmail(
                    _mockMessagingConfig.NpwdEmail,
                    _mockMessagingConfig.NpwdEmailTemplateId,
                    It.IsAny<Dictionary<string, object>>(), null, null, null))
                .Returns(response);

            var emailService = CreateEmailService();

            // Act
            emailService.SendEmailToNpwd("Test error message");

            // Assert
            _mockNotificationClient.Verify(client => client.SendEmail(
                _mockMessagingConfig.NpwdEmail,
                _mockMessagingConfig.NpwdEmailTemplateId,
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey("emailAddress") &&
                    parameters.ContainsKey("applicationName") &&
                    parameters["applicationName"].Equals(Constants.Constants.ApplicationName) &&
                    parameters.ContainsKey("logId") &&
                    parameters.ContainsKey("errorMessage") &&
                    parameters["errorMessage"].Equals("Test error message")), null, null, null),
                Times.Once);

            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Email sent to NPWD")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));
        }

        [Fact]
        public void SendEmailToNpwd_ShouldLogError_WhenExceptionIsThrown()
        {
            // Arrange
            _mockNotificationClient
                .Setup(client => client.SendEmail(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(), null, null, null))
                .Throws(new Exception("Test exception"));

            var emailService = CreateEmailService();

            // Act
            Action act = () => emailService.SendEmailToNpwd("Test error message");

            // Assert
            act.Should().NotThrow(); // Ensure the method handles exceptions internally

            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("GOV UK NOTIFY ERROR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));
        }

        [Fact]
        public void Constructor_ShouldInitializeDependencies()
        {
            // Act
            var emailService = CreateEmailService();

            // Assert
            emailService.Should().NotBeNull();
        }

        [Fact]
        public void SendEmailToNpwd_ShouldNotThrow_WhenNpwdEmailAddressIsNull()
        {
            // Arrange
            _mockMessagingConfig.NpwdEmail = null;
            var emailService = CreateEmailService();

            // Act
            Action act = () => emailService.SendEmailToNpwd("Test error message");

            // Assert
            act.Should().NotThrow();
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("GOV UK NOTIFY ERROR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));
        }

        [Fact]
        public void SendEmailToNpwd_ShouldNotThrow_WhenTemplateIdIsNull()
        {
            // Arrange
            _mockMessagingConfig.NpwdEmailTemplateId = null;
            var emailService = CreateEmailService();

            // Act
            Action act = () => emailService.SendEmailToNpwd("Test error message");

            // Assert
            act.Should().NotThrow();
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("GOV UK NOTIFY ERROR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));
        }

    }
}
