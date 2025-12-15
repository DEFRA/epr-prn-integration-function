using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Models;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class PrnExtensionsTests
{
    [Fact]
    public void ShouldFilterPrns()
    {
        var prns = new Fixture().CreateMany<UpdatedPrnsResponseModel>(5).ToList();
        prns[0].SourceSystemId = null;
        prns[1].SourceSystemId = "blah";
        prns[2].SourceSystemId = "    ";
        prns[3].SourceSystemId = "";
        prns[4].SourceSystemId = "blahblahblah";
        var expectedReExPrns = new List<UpdatedPrnsResponseModel>{prns[1], prns[4]};
        var expectedNpwdPrns = prns.Except(expectedReExPrns);
        prns.FilterNpwdPrns().Should().BeEquivalentTo(expectedNpwdPrns);
        prns.FilterReExPrns().Should().BeEquivalentTo(expectedReExPrns);
    }
}