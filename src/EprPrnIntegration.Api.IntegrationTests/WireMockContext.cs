using RestEase;
using WireMock.Client;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class WireMockContext : IAsyncLifetime
{
    public static string BaseUri => "http://localhost:9090";

    public IWireMockAdminApi WireMockAdminApi { get; } = RestClient.For<IWireMockAdminApi>(BaseUri);
    
    public async Task InitializeAsync()
    {
        await WireMockAdminApi.ResetMappingsAsync();
        await WireMockAdminApi.ResetRequestsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}