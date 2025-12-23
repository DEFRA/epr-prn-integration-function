namespace EprPrnIntegration.Common.UnitTests.Helpers;

public class HttpClientFactoryMock : IHttpClientFactory
{
    private readonly HttpClient _httpClient;

    public HttpClientFactoryMock(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }
}
