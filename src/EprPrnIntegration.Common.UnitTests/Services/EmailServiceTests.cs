using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notify.Interfaces;
using Notify.Models.Responses;

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
        new EmailService(_mockNotificationClient.Object, _mockMessagingConfig.Object, _mockLogger.Object);

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
        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) =>
                state.ToString().Contains("Email sent to John Doe with email address producer1@example.com and the responseid is responseId")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
        ), Times.Once);
    }

    [Fact]
    public void SendEmailsToProducers_LogsError_WhenSendEmailFails()
    {
        // Arrange
        var producerEmails = new List<ProducerEmail>
        {
            new ProducerEmail
            {
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
            It.Is<It.IsAnyType>((state, type) =>
                state.ToString().Contains("GOV UK NOTIFY ERROR. Method: SendEmail")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
        ), Times.Once);
    }

    [Fact]
    public void SendEmailsToProducers_UsesCorrectTemplateId_WhenIsExporterIsTrue()
    {
        // Arrange
        var producerEmails = new List<ProducerEmail>
        {
            new ProducerEmail
            {
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
            new ProducerEmail
            {
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
            It.Is<It.IsAnyType>((state, type) =>
                state.ToString().Contains("Email sent to NPWD with email address")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
        ), Times.Once);
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
            It.Is<It.IsAnyType>((state, type) =>
                state.ToString().Contains("GOV UK NOTIFY ERROR")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
        ), Times.Once);
    }

    [Fact]
    public void SendValidationErrorPrnEmail_SendsEmailWithAttachment_Successfully()
    {
        // Arrange
        var csvData = "Sample CSV Content";
        var reportDate = new DateTime(2025, 1, 1);
        var mockLinkToFile = "https://mock-link-to-file";
        var expectedResponse = new EmailNotificationResponse { id = "responseId" };

        _mockNotificationClient.Setup(client => client.SendEmail(
                It.Is<string>(email => email == _messagingConfig.NpwdEmail),
                It.Is<string>(template => template == _messagingConfig.NpwdValidationErrorsTemplateId),
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey("reportDate") &&
                    parameters.ContainsKey("link_to_file") &&
                    parameters["reportDate"].Equals(reportDate)),
                null, null, null))
            .Returns(expectedResponse);

        // Act
        _emailService.SendValidationErrorPrnEmail(csvData, reportDate);

        // Assert
        _mockNotificationClient.Verify(client => client.SendEmail(
            It.Is<string>(email => email == _messagingConfig.NpwdEmail),
            It.Is<string>(template => template == _messagingConfig.NpwdValidationErrorsTemplateId),
            It.IsAny<Dictionary<string, object>>(), null, null, null), Times.Once);

        _mockLogger.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) =>
                state.ToString().Contains("Validation Error email sent to NPWD with email address")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
        ), Times.Once);
    }

    [Fact]
    public void SendValidationErrorPrnEmail_LogsError_WhenSendEmailFails()
    {
        // Arrange
        var csvData = "Sample CSV Content";
        var reportDate = new DateTime(2025, 1, 1);
        var expectedTemplateId = _messagingConfig.NpwdValidationErrorsTemplateId;
        var exceptionMessage = "Error sending email";

        _mockNotificationClient.Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                null, null, null))
            .Throws(new Exception("Error sending email"));

        // Act
        var exception = Assert.Throws<Exception>(() =>
            _emailService.SendValidationErrorPrnEmail(csvData, reportDate));

        // Assert
        Assert.Equal(exceptionMessage, exception.Message);

        _mockLogger.Verify(logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) =>
                        state.ToString().Contains($"Failed to send email to {_messagingConfig.NpwdEmail} using template ID {_messagingConfig.NpwdValidationErrorsTemplateId}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
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
       
        _mockLogger.Verify(logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) =>
                        state.ToString().Contains("Reconciliation email sent to NPWD")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public void SendIssuedPrnsReconciliationEmailToNpwd_LogsError_WhenSendEmailFails()
    {
        // Arrange
        var exceptionMessage = "Error sending email";
        _mockNotificationClient
            .Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                null, null, null))
            .Throws(new Exception(exceptionMessage));

        // Act
        var exception = Assert.Throws<Exception>(() =>
            _emailService.SendIssuedPrnsReconciliationEmailToNpwd(
                new DateTime(2025, 12, 1),
                0,
                "Sample CSV Content"));

        // Assert
        Assert.Equal(exceptionMessage, exception.Message);

        _mockLogger.Verify(logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) =>
                        state.ToString().Contains($"Failed to send email to {_messagingConfig.NpwdEmail} using template ID {_messagingConfig.NpwdReconcileIssuedPrnsTemplateId}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public void SendUpdatedPrnsReconciliationEmailToNpwd_SendsEmailWithAttachment_Successfully()
    {
        // Arrange
        var expectedResponse = new EmailNotificationResponse { id = "responseId" };

        _mockNotificationClient.Setup(client => client.SendEmail(
                It.Is<string>(email => email == _messagingConfig.NpwdEmail),
                It.Is<string>(template => template == _messagingConfig.NpwdReconcileUpdatedPrnsTemplateId),
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey("date") &&
                    parameters.ContainsKey("link_to_file")),
                null, null, null))
            .Returns(expectedResponse);

        // Act
        _emailService.SendUpdatedPrnsReconciliationEmailToNpwd(new DateTime(2025, 12, 1), "Sample CSV Content");

        // Assert
        _mockNotificationClient.Verify(client => client.SendEmail(
            It.Is<string>(email => email == _messagingConfig.NpwdEmail),
            It.Is<string>(template => template == _messagingConfig.NpwdReconcileUpdatedPrnsTemplateId),
            It.IsAny<Dictionary<string, object>>(), null, null, null), Times.Once);

        _mockLogger.Verify(logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) =>
                        state.ToString().StartsWith("Reconciliation email sent to NPWD")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );

    }

    [Fact]
    public void SendUpdatedPrnsReconciliationEmailToNpwd_LogsError_WhenSendEmailFails()
    {
        // Arrange
        var exceptionMessage = "Error sending email";
        _mockNotificationClient
            .Setup(client => client.SendEmail(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                null, null, null))
            .Throws(new Exception(exceptionMessage));

        // Act
        var exception = Assert.Throws<Exception>(() =>
            _emailService.SendUpdatedPrnsReconciliationEmailToNpwd(
                new DateTime(2025, 12, 1),
                "Sample CSV Content"));

        // Assert
        Assert.Equal(exceptionMessage, exception.Message);
       
        _mockLogger.Verify(logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) =>
                        state.ToString().StartsWith($"Failed to send email to {_messagingConfig.NpwdEmail} using template ID {_messagingConfig.NpwdReconcileUpdatedPrnsTemplateId}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }
}