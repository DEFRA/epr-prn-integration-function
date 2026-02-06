using System.Text;
using FluentAssertions;
using Xunit.Abstractions;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class AzureFunctionInvokerContext
{
    private static readonly HttpClient HttpClient;

    static AzureFunctionInvokerContext()
    {
        HttpClient = new HttpClient { BaseAddress = new Uri($"{BaseUri}/admin/functions/") };

        HttpClient.DefaultRequestHeaders.Add("x-functions-key", "this-is-a-dummy-value");
    }

    private static string BaseUri => "http://localhost:7234";

    public static async Task<DateTime> InvokeAzureFunction(string functionName, ITestOutputHelper? testOutputHelper = null)
    {
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var isRunning = await FunctionExecutionContext.IsRunning(functionName);
            testOutputHelper?.WriteLine(isRunning ? "Running" : "Not Running");
            isRunning.Should().BeFalse();
        });

        var lastUpdate = await FunctionExecutionContext.LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
        
        var request = new HttpRequestMessage(HttpMethod.Post, functionName)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        var response = await HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return lastUpdate;
    }

    public static async Task<HttpResponseMessage> Get(string requestUri) =>
        await HttpClient.GetAsync(requestUri);
}
