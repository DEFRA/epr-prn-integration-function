using RestEase;
using WireMock.Client;

namespace EprPrnIntegration.Api.IntegrationTests;

public class WireMockFixture
{
    public WireMockFixture()
    {
        WireMockAdminApi.ResetMappingsAsync().GetAwaiter().GetResult();
        WireMockAdminApi.ResetRequestsAsync().GetAwaiter().GetResult();
    }

    public static string BaseUri => "http://localhost:9090";

    public IWireMockAdminApi WireMockAdminApi { get; } = RestClient.For<IWireMockAdminApi>(BaseUri);
}