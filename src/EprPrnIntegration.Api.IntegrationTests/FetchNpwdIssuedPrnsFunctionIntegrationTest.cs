using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class FetchNpwdIssuedPrnsFunctionIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private AzureFunctionInvokerContext _azureFunctionInvokerContext = null!;
    private WireMockContext _wireMockContext = null!;

    public async Task InitializeAsync()
    {
        _wireMockContext = new WireMockContext();
        await _wireMockContext.InitializeAsync();

        _azureFunctionInvokerContext = new AzureFunctionInvokerContext();
    }

    public async Task DisposeAsync()
    {
        await _wireMockContext.DisposeAsync();
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsIssuedPrnsToPrnService()
    {
        await Task.WhenAll(
            _wireMockContext.NpwdHasIssuedPrns("ACC123456"),
            _wireMockContext.AccountServiceValidatesIssuedEpr(),
            _wireMockContext.PrnApiAcceptsPrnDetails(),
            _wireMockContext.AccountServiceHasPersonEmailForEpr()
        );

        await _azureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchNpwdIssuedPrnsFunction);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await _wireMockContext.GetPrnDetailRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("ACC123456"));
        });
    }
}