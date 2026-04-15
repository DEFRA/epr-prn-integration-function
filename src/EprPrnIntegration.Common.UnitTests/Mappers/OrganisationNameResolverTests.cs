using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Mappers;

public class OrganisationNameResolverTests
{
    [Fact]
    public void FallbackMapping_ShouldLogWarning()
    {
        var id = Guid.NewGuid();
        var source = CreateSource(id);
        string? warning = null;

        Map(source, x => warning = x);

        warning?.Should().Be($"Fallback trading name or name mapping for organisation {id}");
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
        var source = CreateSource(tradingName: "trading name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Cancelled,
                Type = WoApiOrganisationType.LargeProducer,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("trading name");
    }

    [Theory]
    [InlineData(null, "trading name", "trading name")]
    [InlineData("name", null, "name")]
    public void WhenComplianceScheme_ShouldBeExpected(string? name, string? tradingName, string expected)
    {
        var source = CreateSource(name: name, tradingName: tradingName, year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.ComplianceScheme,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be(expected);
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
    
    [Fact]
    public void WhenNoMatchingRegistrationsForYear_ShouldFallback()
    {
        var source = CreateSource(name: "name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Cancelled,
                Type = WoApiOrganisationType.ComplianceScheme,
                RegistrationYear = 2027
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("name");
    }
    
    [Fact]
    public void WhenComplianceSchemeAndLargeProducerInSameYear_ShouldUseComplianceScheme()
    {
        var source = CreateSource(tradingName: "trading name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.ComplianceScheme,
                RegistrationYear = 2026
            },
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.LargeProducer,
                RegistrationYear = 2026
            }
        ]);
        
        var result = Map(source);
        
        result.Should().Be("trading name");
    }
    
    [Fact]
    public void WhenRegistrationForMultipleYears_ShouldUseComplianceScheme()
    {
        var source = CreateSource(tradingName: "trading name", year: 2026, registrations:
        [
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.LargeProducer,
                RegistrationYear = 2025
            },
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.ComplianceScheme,
                RegistrationYear = 2026
            },
            new WoApiRegistration
            {
                Status = WoApiOrganisationStatus.Registered,
                Type = WoApiOrganisationType.LargeProducer,
                RegistrationYear = 2027
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

    private static string? Map(PackagingRecyclingNote source, Action<string>? logWarning = null)
    {
        logWarning ??= x => { };
        
        return RrepwMappers.Map(source, logWarning).OrganisationName;
    }
}