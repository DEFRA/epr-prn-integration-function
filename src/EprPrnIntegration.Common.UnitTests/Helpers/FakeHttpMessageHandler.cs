using EprPrnIntegration.Api.Models;
using System.Text.Json;

namespace EprPrnIntegration.Common.UnitTests.Helpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<PersonEmail> _response;

    public FakeHttpMessageHandler(List<PersonEmail> response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var jsonResponse = JsonSerializer.Serialize(_response);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(httpResponse);
    }
}

public class MockHttpMessageHandler(
    string responseContent = "",
    System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(responseContent)
        };

        return Task.FromResult(httpResponse);
    }

}