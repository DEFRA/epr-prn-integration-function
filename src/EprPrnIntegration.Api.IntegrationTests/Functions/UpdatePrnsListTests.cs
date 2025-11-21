using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdatePrnsListTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await Task.WhenAll(
            WireMockContext.PrnApiHasUpdateFor("PRN001234567"),
            WireMockContext.PrnApiAcceptsSyncStatus(),
            WireMockContext.NpwdAcceptsPrnPatch());

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdatePrnsList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await WireMockContext.GetNpwdPrnPatchRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await WireMockContext.GetPrnUpdateSyncStatusRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });
    }
}