using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdatePrnsListTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await Task.WhenAll(
            PrnApiStub.HasUpdateFor("PRN001234567"),
            PrnApiStub.AcceptsSyncStatus(),
            NpwdApiStub.AcceptsPrnPatch());

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdatePrnsList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await NpwdApiStub.GetPrnPatchRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await PrnApiStub.GetUpdateSyncStatusRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });
    }
}