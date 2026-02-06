using System.Text;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class FunctionContext
{
    private static readonly HttpClient HttpClient;

    static FunctionContext()
    {
        HttpClient = new HttpClient { BaseAddress = new Uri($"{BaseUri}/admin/functions/") };

        HttpClient.DefaultRequestHeaders.Add("x-functions-key", "this-is-a-dummy-value");
    }

    private static string BaseUri => "http://localhost:7234";

    public static async Task Invoke(string functionName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, functionName)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        var response = await HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<HttpResponseMessage> Get(string requestUri) =>
        await HttpClient.GetAsync(requestUri);
}
