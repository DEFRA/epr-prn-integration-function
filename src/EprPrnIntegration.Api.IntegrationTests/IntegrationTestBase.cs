using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

[Trait("Category", "IntegrationTest")]
[Collection("Integration Tests")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected WireMockContext WireMockContext = null!;

    public async Task InitializeAsync()
    {
        WireMockContext = new WireMockContext();
        
        await WireMockContext.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await WireMockContext.DisposeAsync();
    }
}