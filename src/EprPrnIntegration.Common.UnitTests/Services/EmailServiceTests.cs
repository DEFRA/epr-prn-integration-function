using Moq;
using Xunit;
using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Service;
using EprPrnIntegration.Common.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Notify.Interfaces;
using Notify.Models;
using Notify.Models.Responses;

namespace EprPrnIntegration.Common.UnitTests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<INotificationClient> _mockNotificationClient;
        private readonly Mock<IOptions<MessagingConfig>> _mockMessagingConfig;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;
        private readonly MessagingConfig _messagingConfig;
        private readonly IOptions<MessagingConfig> _mockOptions;

        public EmailServiceTests()
        {
            // Initialize the Mock objects
            _mockNotificationClient = new Mock<INotificationClient>();
            _mockLogger = new Mock<ILogger<EmailService>>();

            // Initialize MessagingConfig with necessary values
            _messagingConfig = new MessagingConfig
            {
                ApiKey = "api-key",
                PrnTemplateId = "prnTemplateId",
                PernTemplateId = "pernTemplateId"
            };

            // Setup the mock IOptions<MessagingConfig> to return the proper MessagingConfig
            _mockMessagingConfig = new Mock<IOptions<MessagingConfig>>();
            _mockMessagingConfig.Setup(m => m.Value).Returns(_messagingConfig);

            // Instantiate the EmailService with the mock dependencies
            _emailService = new EmailService(_mockNotificationClient.Object, _mockMessagingConfig.Object, _mockLogger.Object);
        }

        private EmailService CreateEmailService() =>
            new EmailService(_mockNotificationClient.Object, _mockOptions, _mockLogger.Object);

        [Fact]
        public void SendEmailsToProducers_SuccessfullySendsEmails_LogsInformation()
        {
            // Arrange
            var producerEmails = new List<ProducerEmail>
            {
                new ProducerEmail
                {
                    EmailAddress = "producer1@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    IsPrn = true,
                    PrnNumber = "12345",
                    Material = "Plastic",
                    Tonnage = 100,
                    NameOfExporterReprocessor = "Exporter Ltd",
                    NameOfProducerComplianceScheme = "Compliance Scheme 1"
                }
            };
            var organisationId = "org123";
            var expectedResponse = new EmailNotificationResponse { id = "responseId" };

            _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
                .Returns(expectedResponse);

            var emailService = CreateEmailService();

            // Act
            _emailService.SendEmailsToProducers(producerEmails, organisationId);

            // Assert
            _mockNotificationClient.Verify(client => client.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), null, null, null), Times.Once);
            _mockLogger.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("Email sent to John Doe with email address producer1@example.com and the responseid is responseId"))), Times.Once);
        }

        [Fact]
        public void SendEmailsToProducers_LogsError_WhenSendEmailFailsold()
        {
            // Arrange
            var producerEmails = new List<ProducerEmail>
            {
                new ProducerEmail
                {
                    EmailAddress = "producer2@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    IsPrn = false,
                    PrnNumber = "67890",
                    Material = "Metal",
                    Tonnage = 200,
                    NameOfExporterReprocessor = "Exporter Inc",
                    NameOfProducerComplianceScheme = "Compliance Scheme 2"
                }
            };
            var organisationId = "org456";

            _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
                .Throws(new Exception("Error sending email"));

            // Act
            _emailService.SendEmailsToProducers(producerEmails, organisationId);

            // Assert
            _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("GOV UK NOTIFY ERROR. Method: SendEmail"))), Times.Once);
        }

        [Fact]
        public void SendEmailsToProducers_UsesCorrectTemplateId_WhenIsPrnIsTrue()
        {
            // Arrange
            var producerEmails = new List<ProducerEmail>
            {
                new ProducerEmail
                {
                    EmailAddress = "producer3@example.com",
                    FirstName = "Mark",
                    LastName = "Taylor",
                    IsPrn = true,
                    PrnNumber = "54321",
                    Material = "Wood",
                    Tonnage = 150,
                    NameOfExporterReprocessor = "Exporter Corp",
                    NameOfProducerComplianceScheme = "Compliance Scheme 3"
                }
            };
            var organisationId = "org789";

            _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                "prnTemplateId", // Expect the PRN template
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
                .Returns(new EmailNotificationResponse { id = "responseId" });

            // Act
            _emailService.SendEmailsToProducers(producerEmails, organisationId);

            // Assert
            _mockNotificationClient.Verify(client => client.SendEmail(It.IsAny<string>(), "prnTemplateId", It.IsAny<Dictionary<string, dynamic>>(), null, null, null), Times.Once);
        }

        [Fact]
        public void SendEmailsToProducers_UsesCorrectTemplateId_WhenIsPrnIsFalse()
        {
            // Arrange
            var producerEmails = new List<ProducerEmail>
            {
                new ProducerEmail
                {
                    EmailAddress = "producer4@example.com",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    IsPrn = false,
                    PrnNumber = "98765",
                    Material = "Glass",
                    Tonnage = 50,
                    NameOfExporterReprocessor = "Exporter LLC",
                    NameOfProducerComplianceScheme = "Compliance Scheme 4"
                }
            };
            var organisationId = "org101112";

            _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                "pernTemplateId", // Expect the PERN template
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
                .Returns(new EmailNotificationResponse { id = "responseId" });

            // Act
            _emailService.SendEmailsToProducers(producerEmails, organisationId);

            // Assert
            _mockNotificationClient.Verify(client => client.SendEmail(It.IsAny<string>(), "pernTemplateId", It.IsAny<Dictionary<string, dynamic>>(), null, null, null), Times.Once);
        }

    }
}
