using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class HealthCheckTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenHealthCheckRequest_ShouldBeSuccess()
    {
        var result = await AzureFunctionInvokerContext.Get("/api/health");

        result.EnsureSuccessStatusCode();
    }
}