using System.Text;

namespace EprPrnIntegration.Api.IntegrationTests;

public enum FunctionName
{
    UpdateProducersList,
    UpdatePrnsList,
    FetchNpwdIssuedPrnsFunction,
    UpdateWasteOrganisations,
    FetchRrepwIssuedPrns,
    UpdateRrepwPrnsList,
}

public static class AzureFunctionInvokerContext
{
    private static readonly HttpClient HttpClient;

    static AzureFunctionInvokerContext()
    {
        HttpClient = new HttpClient { BaseAddress = new Uri($"{BaseUri}/admin/functions/") };

        HttpClient.DefaultRequestHeaders.Add("x-functions-key", "this-is-a-dummy-value");
    }

    private static string BaseUri => "http://localhost:7234";

    public static async Task InvokeAzureFunction(FunctionName functionName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, functionName.ToString())
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        var response = await HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<HttpResponseMessage> Get(string requestUri) =>
        await HttpClient.GetAsync(requestUri);
}
