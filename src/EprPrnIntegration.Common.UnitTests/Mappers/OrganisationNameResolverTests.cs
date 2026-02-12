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

        Subject.Resolve(CreateSource(id), new SavePrnDetailsRequest(), destMember: null, context: null!);

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
        var result = Subject.Resolve(
            CreateSource(name: name, tradingName: tradingName), 
            new SavePrnDetailsRequest(),
            destMember: null, 
            context: null!);
        
        result.Should().Be(expected);
    }

    private static PackagingRecyclingNote CreateSource(Guid? id = null, string? name = null, string? tradingName = null)
    {
        return new PackagingRecyclingNote
        {
            Organisation = new WoApiOrganisation
            {
                Id = id ?? Guid.NewGuid(),
                Address = null!,
                Registrations = null!
            },
            IssuedToOrganisation = new Organisation
            {
                Name = name,
                TradingName = tradingName
            }
        };
    }
}