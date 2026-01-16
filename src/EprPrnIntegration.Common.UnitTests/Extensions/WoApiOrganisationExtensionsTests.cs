using EprPrnIntegration.Common.Extensions;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EprPrnIntegration.Common.UnitTests.Extensions;

public class WoApiOrganisationExtensionsTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly ILogger _logger;

    public WoApiOrganisationExtensionsTests()
    {
        _loggerMock = new Mock<ILogger>();
        _logger = _loggerMock.Object;
    }

    private void AssertNoErrorLogged()
    {
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never
        );
    }

    private void AssertErrorLogged()
    {
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void GetEntityTypeCode_WhenOrganisationIsComplianceScheme_ReturnsComplianceSchemeCode()
    {
        // Arrange
        var organisation = new WoApiOrganisation
        {
            Id = Guid.NewGuid(),
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Registered,
                    Type = WoApiOrganisationType.ComplianceScheme,
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().Be(OrganisationType.ComplianceScheme_CS);
        AssertNoErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenOrganisationIsLargeProducer_ReturnsLargeProducerCode()
    {
        // Arrange
        var organisation = new WoApiOrganisation
        {
            Id = Guid.NewGuid(),
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Registered,
                    Type = WoApiOrganisationType.LargeProducer,
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().Be(OrganisationType.LargeProducer_DR);
        AssertNoErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenOrganisationHasBothTypes_ReturnsNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var organisation = new WoApiOrganisation
        {
            Id = organisationId,
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Registered,
                    Type = WoApiOrganisationType.ComplianceScheme,
                },
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Registered,
                    Type = WoApiOrganisationType.LargeProducer,
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().BeNull();
        AssertErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenOrganisationHasNoValidRegistrations_ReturnsNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var organisation = new WoApiOrganisation
        {
            Id = organisationId,
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Registered,
                    Type = "SmallProducer", // Not CS or LP
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().BeNull();
        AssertErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenNoRegistrationsForYear_ReturnsNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var organisation = new WoApiOrganisation
        {
            Id = organisationId,
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2024, // Different year
                    Status = WoApiOrganisationStatus.Registered,
                    Type = WoApiOrganisationType.ComplianceScheme,
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().BeNull();
        AssertErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenRegistrationIsNotRegisteredStatus_ReturnsNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var organisation = new WoApiOrganisation
        {
            Id = organisationId,
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Cancelled, // Not Registered
                    Type = WoApiOrganisationType.ComplianceScheme,
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().BeNull();
        AssertErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenMultipleRegistrationsForSameYear_UsesCorrectOne()
    {
        // Arrange
        var organisation = new WoApiOrganisation
        {
            Id = Guid.NewGuid(),
            Address = new WoApiAddress { },
            Registrations =
            [
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Cancelled,
                    Type = WoApiOrganisationType.LargeProducer,
                },
                new WoApiRegistration
                {
                    RegistrationYear = 2025,
                    Status = WoApiOrganisationStatus.Registered, // Only this should count
                    Type = WoApiOrganisationType.ComplianceScheme,
                },
                new WoApiRegistration
                {
                    RegistrationYear = 2024,
                    Status = WoApiOrganisationStatus.Registered,
                    Type = WoApiOrganisationType.LargeProducer,
                },
            ],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().Be(OrganisationType.ComplianceScheme_CS);
        AssertNoErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenOrganisationHasEmptyRegistrations_ReturnsNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var organisation = new WoApiOrganisation
        {
            Id = organisationId,
            Address = new WoApiAddress { },
            Registrations = [],
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().BeNull();
        AssertErrorLogged();
    }

    [Fact]
    public void GetEntityTypeCode_WhenRegistrationsIsNull_ReturnsNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var organisation = new WoApiOrganisation
        {
            Id = organisationId,
            Address = new WoApiAddress { },
            Registrations = null!,
        };

        // Act
        var result = organisation.GetEntityTypeCode(2025, _logger);

        // Assert
        result.Should().BeNull();
        AssertErrorLogged();
    }
}
