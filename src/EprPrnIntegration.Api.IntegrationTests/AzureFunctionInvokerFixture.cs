using System.Net.Http.Headers;
using System.Text;

namespace EprPrnIntegration.Api.IntegrationTests;

public enum FunctionName
{
    UpdateProducersList,
    UpdatePrnsList
}
public class AzureFunctionInvokerFixture
{
    private readonly HttpClient _httpClient;
    private static string BaseUri => "http://localhost:5800";
    public AzureFunctionInvokerFixture()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{BaseUri}/admin/functions/"),
        };

        _httpClient.DefaultRequestHeaders.Add("x-functions-key", "this-is-a-dummy-value");
    }

    public async Task InvokeAzureFunction(FunctionName functionName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, functionName.ToString())
        {
            Content = new StringContent(
                "{}",
                Encoding.UTF8,
                "application/json"
            )
        }; 
        
        var response = await _httpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
    }
}