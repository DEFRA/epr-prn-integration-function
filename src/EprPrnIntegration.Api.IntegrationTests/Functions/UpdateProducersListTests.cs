using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateProducersListTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await CommonDataApiStub.HasUpdateFor("Acme Manufacturing Ltd");

        await NpwdApiStub.AcceptsProducerPatch();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateProducersList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await NpwdApiStub.GetProducersPatchRequests();

            Assert.Contains(
                requests,
                entry => entry.Request.Body!.Contains("Acme Manufacturing Ltd")
            );
        });
    }
}
