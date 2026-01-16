using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Api.Services;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests.Services;

public class ProducerEmailServiceTests
{
    private readonly ProducerEmailService _service;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IOrganisationService> _organisationServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private const int Year = 2025;

    public ProducerEmailServiceTests()
    {
        _service = new ProducerEmailService();
        _loggerMock = new Mock<ILogger>();
        _organisationServiceMock = new Mock<IOrganisationService>();
        _emailServiceMock = new Mock<IEmailService>();
    }

    [Fact]
    public async Task SendEmailToProducersAsync_WhenOrganisationIsNull_LogsErrorAndReturns()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            null,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) =>
                            o.ToString()!.Contains("Cannot send email to producer")
                            && o.ToString()!.Contains("IssueToOrganisation.Id is null")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _emailServiceMock.VerifyNoOtherCalls();
        _organisationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendEmailToProducersAsync_WhenAccreditationYearIsInvalid_LogsErrorAndReturns()
    {
        // Arrange
        var request = CreateRequest();
        request.AccreditationYear = "not-a-year";
        var organisation = CreateOrganisation();

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) =>
                            o.ToString()!.Contains("Cannot send email to producer")
                            && o.ToString()!.Contains("AccreditationYear is not valid")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _emailServiceMock.VerifyNoOtherCalls();
        _organisationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendEmailToProducersAsync_WhenEntityTypeCodeIsNull_LogsErrorAndReturns()
    {
        // Arrange
        var request = CreateRequest();
        var organisation = new WoApiOrganisation
        {
            Id = Guid.NewGuid(),
            Address = new WoApiAddress(),
            Registrations = [], // No registrations will cause GetEntityTypeCode to return null
        };

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) =>
                            o.ToString()!.Contains("Cannot send email to producer")
                            && o.ToString()!.Contains("failed to get issuedToEntityTypeCode")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _emailServiceMock.VerifyNoOtherCalls();
        _organisationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendEmailToProducersAsync_WhenStatusIsCancelled_SendsCancelledNotification()
    {
        // Arrange
        var request = CreateRequest();
        request.PrnStatusId = (int)EprnStatus.CANCELLED;
        var organisation = CreateOrganisation();
        var producerEmails = new List<PersonEmail>
        {
            new()
            {
                Email = "test1@example.com",
                FirstName = "John",
                LastName = "Doe",
            },
            new()
            {
                Email = "test2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
            },
        };

        _organisationServiceMock
            .Setup(x =>
                x.GetPersonEmailsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(producerEmails);

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        _emailServiceMock.Verify(
            x =>
                x.SendCancelledPrnsNotificationEmails(
                    It.Is<List<ProducerEmail>>(list => list.Count == 2),
                    organisation.Id.ToString()
                ),
            Times.Once
        );

        _emailServiceMock.Verify(
            x => x.SendEmailsToProducers(It.IsAny<List<ProducerEmail>>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailToProducersAsync_WhenStatusIsNotCancelled_SendsRegularNotification()
    {
        // Arrange
        var request = CreateRequest();
        request.PrnStatusId = (int)EprnStatus.AWAITINGACCEPTANCE;
        var organisation = CreateOrganisation();
        var producerEmails = new List<PersonEmail>
        {
            new()
            {
                Email = "test1@example.com",
                FirstName = "John",
                LastName = "Doe",
            },
        };

        _organisationServiceMock
            .Setup(x =>
                x.GetPersonEmailsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(producerEmails);

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        _emailServiceMock.Verify(
            x =>
                x.SendEmailsToProducers(
                    It.Is<List<ProducerEmail>>(list => list.Count == 1),
                    organisation.Id.ToString()
                ),
            Times.Once
        );

        _emailServiceMock.Verify(
            x =>
                x.SendCancelledPrnsNotificationEmails(
                    It.IsAny<List<ProducerEmail>>(),
                    It.IsAny<string>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailToProducersAsync_WhenExceptionThrown_LogsError()
    {
        // Arrange
        var request = CreateRequest();
        var organisation = CreateOrganisation();
        var expectedException = new Exception("Test exception");

        _organisationServiceMock
            .Setup(x =>
                x.GetPersonEmailsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(expectedException);

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) => o.ToString()!.Contains("Failed to send email notification")
                    ),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailToProducersAsync_MapsProducerEmailFieldsCorrectly()
    {
        // Arrange
        var request = CreateRequest();
        request.IssuedByOrg = "Exporter Org";
        request.OrganisationName = "Producer Org";
        request.PrnNumber = "PRN-123";
        request.MaterialName = "Plastic";
        request.TonnageValue = 100;
        request.IsExport = true;

        var organisation = CreateOrganisation();
        var producerEmails = new List<PersonEmail>
        {
            new()
            {
                Email = "producer@example.com",
                FirstName = "Alice",
                LastName = "Johnson",
            },
        };

        _organisationServiceMock
            .Setup(x =>
                x.GetPersonEmailsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(producerEmails);

        ProducerEmail? capturedProducerEmail = null;
        _emailServiceMock
            .Setup(x =>
                x.SendEmailsToProducers(It.IsAny<List<ProducerEmail>>(), It.IsAny<string>())
            )
            .Callback<List<ProducerEmail>, string>(
                (emails, _) =>
                {
                    capturedProducerEmail = emails.FirstOrDefault();
                }
            );

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        capturedProducerEmail.Should().NotBeNull();
        capturedProducerEmail!.EmailAddress.Should().Be("producer@example.com");
        capturedProducerEmail.FirstName.Should().Be("Alice");
        capturedProducerEmail.LastName.Should().Be("Johnson");
        capturedProducerEmail.NameOfExporterReprocessor.Should().Be("Exporter Org");
        capturedProducerEmail.NameOfProducerComplianceScheme.Should().Be("Producer Org");
        capturedProducerEmail.PrnNumber.Should().Be("PRN-123");
        capturedProducerEmail.Material.Should().Be("Plastic");
        capturedProducerEmail.Tonnage.Should().Be(100);
        capturedProducerEmail.IsExporter.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailToProducersAsync_HandlesNullRequestFields()
    {
        // Arrange
        var request = CreateRequest();
        request.IssuedByOrg = null;
        request.OrganisationName = null;
        request.PrnNumber = null;
        request.TonnageValue = null;
        request.IsExport = null;

        var organisation = CreateOrganisation();
        var producerEmails = new List<PersonEmail>
        {
            new()
            {
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
            },
        };

        _organisationServiceMock
            .Setup(x =>
                x.GetPersonEmailsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(producerEmails);

        ProducerEmail? capturedProducerEmail = null;
        _emailServiceMock
            .Setup(x =>
                x.SendEmailsToProducers(It.IsAny<List<ProducerEmail>>(), It.IsAny<string>())
            )
            .Callback<List<ProducerEmail>, string>(
                (emails, _) =>
                {
                    capturedProducerEmail = emails.FirstOrDefault();
                }
            );

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert
        capturedProducerEmail.Should().NotBeNull();
        capturedProducerEmail!.NameOfExporterReprocessor.Should().Be("");
        capturedProducerEmail.NameOfProducerComplianceScheme.Should().Be("");
        capturedProducerEmail.PrnNumber.Should().Be("");
        capturedProducerEmail.Tonnage.Should().Be(0);
        capturedProducerEmail.IsExporter.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailToProducersAsync_LogsInformationMessages()
    {
        // Arrange
        var request = CreateRequest();
        var organisation = CreateOrganisation();
        var producerEmails = new List<PersonEmail>
        {
            new()
            {
                Email = "test1@example.com",
                FirstName = "John",
                LastName = "Doe",
            },
            new()
            {
                Email = "test2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
            },
        };

        _organisationServiceMock
            .Setup(x =>
                x.GetPersonEmailsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(producerEmails);

        // Act
        await _service.SendEmailToProducersAsync(
            request,
            organisation,
            _loggerMock.Object,
            _organisationServiceMock.Object,
            _emailServiceMock.Object
        );

        // Assert - Verify information logs
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Fetched 2 producers")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) =>
                            o.ToString()!.Contains("Sending email notifications to 2 producers")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) => o.ToString()!.Contains("Successfully processed and sent emails")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    private static SavePrnDetailsRequest CreateRequest()
    {
        return new SavePrnDetailsRequest
        {
            PrnNumber = "PRN-001",
            AccreditationYear = Year.ToString(),
            MaterialName = "Plastic",
            TonnageValue = 50,
            IsExport = false,
            IssuedByOrg = "Test Exporter",
            OrganisationName = "Test Producer",
            PrnStatusId = (int)EprnStatus.AWAITINGACCEPTANCE,
        };
    }

    private static WoApiOrganisation CreateOrganisation()
    {
        return new WoApiOrganisation
        {
            Id = Guid.NewGuid(),
            Address = new WoApiAddress(),
            Registrations =
            [
                new WoApiRegistration
                {
                    Type = WoApiOrganisationType.LargeProducer,
                    RegistrationYear = Year,
                    Status = WoApiOrganisationStatus.Registered,
                },
            ],
        };
    }
}
