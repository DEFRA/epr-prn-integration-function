using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class UpdatePrnsListFunctionIntegrationTest : IntegrationTestBase, IAsyncLifetime
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
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await Task.WhenAll(
            _wireMockContext.PrnApiHasUpdateFor("PRN001234567"),
            _wireMockContext.PrnApiAcceptsSyncStatus(),
            _wireMockContext.NpwdAcceptsPrnPatch());

        await _azureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdatePrnsList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await _wireMockContext.GetNpwdPrnPatchRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await _wireMockContext.GetPrnUpdateSyncStatusRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });
    }
}