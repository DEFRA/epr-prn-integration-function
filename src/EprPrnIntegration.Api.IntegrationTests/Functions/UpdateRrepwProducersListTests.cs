using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateRrepwProducersListTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await CommonDataApiStub.HasUpdateFor("Acme Manufacturing Ltd");

        await NpwdApiStub.AcceptsProducerPatch();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwProducersList);
        
        // TODO: Check fake api, when the function makes proper http calls.
    }
}