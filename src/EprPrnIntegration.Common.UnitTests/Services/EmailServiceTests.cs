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
        [Fact]
        public void SendEmailToNpwd_ShouldLogInformation_WhenEmailIsSentSuccessfully()
        {
            // Arrange
            var mockNotificationClient = new Mock<INotificationClient>();
            var mockLogger = new Mock<ILogger<EmailService>>();
            var mockMessagingConfig = new MessagingConfig
            {
                NpwdEmail = "test@example.com",
                NpwdEmailTemplateId = "template123"
            };

            var mockOptions = Mock.Of<IOptions<MessagingConfig>>(opt => opt.Value == mockMessagingConfig);

            var response = new EmailNotificationResponse
            {
                id = "response123"
            };

            mockNotificationClient
                .Setup(client => client.SendEmail(
                    mockMessagingConfig.NpwdEmail,
                    mockMessagingConfig.NpwdEmailTemplateId,
                    It.IsAny<Dictionary<string, object>>(), null,null,null))
                .Returns(response);

            var emailService = new EmailService(mockNotificationClient.Object, mockOptions, mockLogger.Object);

            // Act
            emailService.SendEmailToNpwd("Test error message");

            // Assert
            mockNotificationClient.Verify(client => client.SendEmail(
                mockMessagingConfig.NpwdEmail,
                mockMessagingConfig.NpwdEmailTemplateId,
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey("emailAddress") &&
                    parameters["emailAddress"].Equals(mockMessagingConfig.NpwdEmail) &&
                    parameters.ContainsKey("errorMessage") &&
                    parameters["errorMessage"].Equals("Test error message")
                ),null,null,null), Times.Once);

            mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Email sent to NPWD")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void SendEmailToNpwd_ShouldLogError_WhenExceptionIsThrown()
        {
            // Arrange
            var mockNotificationClient = new Mock<INotificationClient>();
            var mockLogger = new Mock<ILogger<EmailService>>();
            var mockMessagingConfig = new MessagingConfig
            {
                NpwdEmail = "test@example.com",
                NpwdEmailTemplateId = "template123"
            };

            var mockOptions = Mock.Of<IOptions<MessagingConfig>>(opt => opt.Value == mockMessagingConfig);

            mockNotificationClient
                .Setup(client => client.SendEmail(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),null,null,null))
                .Throws(new Exception("Test exception"));

            var emailService = new EmailService(mockNotificationClient.Object, mockOptions, mockLogger.Object);

            // Act
            Action act = () => emailService.SendEmailToNpwd("Test error message");

            // Assert
            act.Should().NotThrow(); // Ensure the method handles exceptions internally

            mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("GOV UK NOTIFY ERROR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }
}
