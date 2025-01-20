using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notify.Interfaces;
using Notify.Models.Responses;
using System.Text;

namespace EprPrnIntegration.Common.UnitTests.Services;

public class EmailServiceTests
{
    private readonly Mock<INotificationClient> _mockNotificationClient;
    private readonly Mock<IOptions<MessagingConfig>> _mockMessagingConfig;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly EmailService _emailService;
    private readonly MessagingConfig _messagingConfig;

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
            PernTemplateId = "pernTemplateId",
            NpwdEmailTemplateId = "npwdEmailTemplateId",
            NpwdValidationErrorsTemplateId = "npwdValidationErrorsTemplateId",
            NpwdReconcileIssuedPrnsTemplateId = "npwdReconcileIssuedPrnsTemplateId",
            NpwdEmail = "npwd@email.com"
        };
            
        // Setup the mock IOptions<MessagingConfig> to return the proper MessagingConfig
        _mockMessagingConfig = new Mock<IOptions<MessagingConfig>>();
        _mockMessagingConfig.Setup(m => m.Value).Returns(_messagingConfig);

        // Instantiate the EmailService with the mock dependencies
        _emailService = new EmailService(_mockNotificationClient.Object, _mockMessagingConfig.Object, _mockLogger.Object);
    }

    private EmailService CreateEmailService() =>
        new(_mockNotificationClient.Object, _mockMessagingConfig.Object, _mockLogger.Object);

    [Fact]
    public void SendEmailsToProducers_SuccessfullySendsEmails_LogsInformation()
    {
        // Arrange
        var producerEmails = new List<ProducerEmail>
        {
            new() {
                EmailAddress = "producer1@example.com",
                FirstName = "John",
                LastName = "Doe",
                IsExporter = true,
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
        _mockLogger.VerifyLog(logger => logger.LogInformation("Email sent to John Doe with email address producer1@example.com and the responseid is responseId."), Times.Once);
    }

    [Fact]
    public void SendEmailsToProducers_LogsError_WhenSendEmailFails()
    {
        // Arrange
        var producerEmails = new List<ProducerEmail>
        {
            new() {
                EmailAddress = "producer2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                IsExporter = false,
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
        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("GOV UK NOTIFY ERROR. Method: SendEmail")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }

    [Fact]
    public void SendEmailsToProducers_UsesCorrectTemplateId_WhenIsExporterIsTrue()
    {
        // Arrange
        var producerEmails = new List<ProducerEmail>
        {
            new() {
                EmailAddress = "producer3@example.com",
                FirstName = "Mark",
                LastName = "Taylor",
                IsExporter = true,
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
                "pernTemplateId", // Expect the PRN template
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
            .Returns(new EmailNotificationResponse { id = "responseId" });

        // Act
        _emailService.SendEmailsToProducers(producerEmails, organisationId);

        // Assert
        _mockNotificationClient.Verify(client => client.SendEmail(It.IsAny<string>(), "pernTemplateId", It.IsAny<Dictionary<string, dynamic>>(), null, null, null), Times.Once);
    }

    [Fact]
    public void SendEmailsToProducers_UsesCorrectTemplateId_WhenIsExporterIsFalse()
    {
        // Arrange
        var producerEmails = new List<ProducerEmail>
        {
            new() {
                EmailAddress = "producer4@example.com",
                FirstName = "Sarah",
                LastName = "Johnson",
                IsExporter = false,
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
                "prnTemplateId", // Expect the PERN template
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
            .Returns(new EmailNotificationResponse { id = "responseId" });

        // Act
        _emailService.SendEmailsToProducers(producerEmails, organisationId);

        // Assert
        _mockNotificationClient.Verify(client => client.SendEmail(It.IsAny<string>(), "prnTemplateId", It.IsAny<Dictionary<string, dynamic>>(), null, null, null), Times.Once);
    }


    [Fact]
    public void SendUpdatePrnsErrorEmailToNpwd_LogsInformation_WhenSendEmailSucceeds()
    {
        // Arrange
        _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
            .Returns(new EmailNotificationResponse { id = "ABC1121" });

        // Act
        _emailService.SendErrorEmailToNpwd(It.IsAny<string>());

        // Assert
        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("Email sent to NPWD with email address")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }

    [Fact]
    public void SendUpdatePrnsErrorEmailToNpwd_LogsError_WhenSendEmailFails()
    {
        // Arrange
        _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(), null, null, null))
            .Throws(new Exception("Error sending email"));

        // Act
        _emailService.SendErrorEmailToNpwd(It.IsAny<string>());

        // Assert
        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("GOV UK NOTIFY ERROR")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }

    [Fact]
    public void SendErrorFetchedPrnEmail_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _emailService.SendValidationErrorPrnEmail(null!, DateTime.UtcNow));
    }

    [Fact]
    public void SendValidationErrorPrnEmail_SendsEmailWithAttachment_Successfully()
    {
        // Arrange
        var mockStream = new MemoryStream(Encoding.UTF8.GetBytes("Sample CSV Content"));
        var reportDate = new DateTime(2025, 1, 1);
        var expectedResponse = new EmailNotificationResponse { id = "responseId" };

        _mockNotificationClient.Setup(client => client.SendEmail(
                It.Is<string>(email => email == _messagingConfig.NpwdEmail),
                It.Is<string>(template => template == _messagingConfig.NpwdValidationErrorsTemplateId),
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey("emailAddress") &&
                    parameters.ContainsKey("reportDate") &&
                    parameters.ContainsKey("link_to_file") &&
                    parameters["emailAddress"].Equals(_messagingConfig.NpwdEmail) &&
                    parameters["reportDate"].Equals(reportDate)),
                null, null, null))
            .Returns(expectedResponse);

        // Act
        _emailService.SendValidationErrorPrnEmail(mockStream, reportDate);

        // Assert
        _mockNotificationClient.Verify(client => client.SendEmail(
            It.Is<string>(email => email == _messagingConfig.NpwdEmail),
            It.Is<string>(template => template == _messagingConfig.NpwdValidationErrorsTemplateId),
            It.IsAny<Dictionary<string, object>>(), null, null, null), Times.Once);

        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("Email sent to NPWD")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }

    [Fact]
    public void SendValidationErrorPrnEmail_LogsError_WhenSendEmailFails()
    {
        // Arrange
        var mockStream = new MemoryStream(Encoding.UTF8.GetBytes("Sample CSV Content"));
        var reportDate = new DateTime(2025, 1, 1);

        _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                null, null, null))
            .Throws(new Exception("Error sending email"));

        // Act
        _emailService.SendValidationErrorPrnEmail(mockStream, reportDate);

        // Assert
        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("Failed to send email to")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }

    [Fact]
    public void SendIssuedPrnsReconciliationEmailToNpwd_SendsEmailWithAttachment_Successfully()
    {
        // Arrange
        var expectedResponse = new EmailNotificationResponse { id = "responseId" };

        _mockNotificationClient.Setup(client => client.SendEmail(
                It.Is<string>(email => email == _messagingConfig.NpwdEmail),
                It.Is<string>(template => template == _messagingConfig.NpwdReconcileIssuedPrnsTemplateId),
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey("report_date") &&
                    parameters.ContainsKey("report_count") &&
                    parameters.ContainsKey("link_to_file")),
                null, null, null))
            .Returns(expectedResponse);

        // Act
        _emailService.SendIssuedPrnsReconciliationEmailToNpwd(new DateTime(2025, 12, 1), 0, "Sample CSV Content");

        // Assert
        _mockNotificationClient.Verify(client => client.SendEmail(
            It.Is<string>(email => email == _messagingConfig.NpwdEmail),
            It.Is<string>(template => template == _messagingConfig.NpwdReconcileIssuedPrnsTemplateId),
            It.IsAny<Dictionary<string, object>>(), null, null, null), Times.Once);

        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("Reconciliation email sent to NPWD")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }

    [Fact]
    public void SendIssuedPrnsReconciliationEmailToNpwd_LogsError_WhenSendEmailFails()
    {
        // Arrange
        _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                null, null, null))
            .Throws(new Exception("Error sending email"));

        // Act
        _emailService.SendIssuedPrnsReconciliationEmailToNpwd(new DateTime(2025, 12, 1), 0, "Sample CSV Content");

        // Assert
        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().StartsWith("Failed to send email to")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
    }
}