using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class FetchNpwdIssuedPrnsFunctionIntegrationTest : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsIssuedPrnsToPrnService()
    {
        await Task.WhenAll(
            WireMockContext.NpwdHasIssuedPrns("ACC123456"),
            WireMockContext.AccountServiceValidatesIssuedEpr(),
            WireMockContext.PrnApiAcceptsPrnDetails(),
            WireMockContext.AccountServiceHasPersonEmailForEpr()
        );

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchNpwdIssuedPrnsFunction);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await WireMockContext.GetPrnDetailRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("ACC123456"));
        });
    }
}