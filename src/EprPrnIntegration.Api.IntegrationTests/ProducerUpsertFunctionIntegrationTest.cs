using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class ProducerUpsertFunctionIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private WireMockContext _wireMockContext = null!;

    public async Task InitializeAsync()
    {
        _wireMockContext = new WireMockContext();
        await _wireMockContext.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _wireMockContext.DisposeAsync();
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await _wireMockContext.CommonDataApiHasUpdateFor("Acme Manufacturing Ltd");

        await _wireMockContext.NpwdAcceptsProducerPatch();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateProducersList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await _wireMockContext.GetNpwdProducersPatchRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("Acme Manufacturing Ltd"));
        });
    }
}