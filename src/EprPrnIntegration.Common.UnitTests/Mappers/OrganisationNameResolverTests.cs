using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Mappers;

public class OrganisationNameResolverTests
{
    private Mock<ILogger<OrganisationNameResolver>> Logger { get; }
    private OrganisationNameResolver Subject { get; }

    public OrganisationNameResolverTests()
    {
        Logger = new Mock<ILogger<OrganisationNameResolver>>();
        Subject = new OrganisationNameResolver(Logger.Object);
    }
    
    [Fact]
    public void FallbackMapping_ShouldLogWarning()
    {
        var id = Guid.NewGuid();
        var source = CreateSource(id);

        Map(source);

        Logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Fallback trading name mapping for organisation {id}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("name", "", "name")]
    [InlineData("name", " ", "name")]
    [InlineData("name", null, "name")]
    [InlineData("name", "trading name", "trading name")]
    public void FallbackMapping_ShouldBeAsExpected(string? name, string? tradingName, string? expected)
    {
        var source = CreateSource(name: name, tradingName: tradingName);
        
        var result = Map(source);
        
        result.Should().Be(expected);
    }

    [Fact]
    public void WhenLargeProducer_ShouldUseName()
    {
        var source = CreateSource(name: "name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.LargeProducer,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("name");
    }
    
    [Fact]
    public void WhenLargeProducer_ButCancelled_ShouldFallback()
    {
        var source = CreateSource(name: "name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Cancelled,
                Type = WoApiOrganisationType.LargeProducer,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("name");
    }

    [Fact]
    public void WhenComplianceScheme_ShouldUseName()
    {
        var source = CreateSource(tradingName: "trading name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.ComplianceScheme,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("trading name");
    }
    
    [Fact]
    public void WhenComplianceScheme_ButCancelled_ShouldFallback()
    {
        var source = CreateSource(tradingName: "trading name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Cancelled,
                Type = WoApiOrganisationType.ComplianceScheme,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("trading name");
    }

    private static PackagingRecyclingNote CreateSource(
        Guid? id = null, 
        string? name = null, 
        string? tradingName = null, 
        int? year = null,
        List<WoApiRegistration>? registrations = null)
    {
        return new PackagingRecyclingNote
        {
            Accreditation = new Accreditation
            {
                AccreditationYear = year
            },
            Organisation = new WoApiOrganisation
            {
                Id = id ?? Guid.NewGuid(),
                Address = null!,
                Registrations = registrations ?? []
            },
            IssuedToOrganisation = new Organisation
            {
                Name = name,
                TradingName = tradingName
            }
        };
    }

    private string? Map(PackagingRecyclingNote source)
    {
        return Subject.Resolve(source, new SavePrnDetailsRequest(), destMember: null, context: null!);
    }
}