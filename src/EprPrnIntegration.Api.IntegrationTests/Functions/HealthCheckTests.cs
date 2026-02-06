using System.Net;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class HealthCheckTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenHealthCheckRequest_ShouldBeSuccess()
    {
        var result = await FunctionContext.Get("/api/health");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
