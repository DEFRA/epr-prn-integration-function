using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class HealthCheckTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenHealthCheckRequest_ShouldBeSuccess()
    {
        var result = await AzureFunctionInvokerContext.Get("/api/health");

        result.EnsureSuccessStatusCode();
    }
}