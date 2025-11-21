using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateProducersListTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await WireMockContext.CommonDataApiHasUpdateFor("Acme Manufacturing Ltd");

        await WireMockContext.NpwdAcceptsProducerPatch();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateProducersList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await WireMockContext.GetNpwdProducersPatchRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("Acme Manufacturing Ltd"));
        });
    }
}