using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

[CollectionDefinition("EPRFunctions")]
public class EPRFunctionsCollection :
    ICollectionFixture<WireMockFixture>,
    ICollectionFixture<AzureFunctionInvokerFixture>
{
    // This class is never instantiated - it's just a marker
}

[Collection("EPRFunctions")]
public class ProducerUpsertFunctionIntegrationTest(WireMockFixture wireMockFixture, AzureFunctionInvokerFixture functionInvoker) : IntegrationTestBase
{
    [Fact]
    public async Task HappyPath()
    {
        
        await functionInvoker.InvokeAzureFunction(FunctionName.UpdateProducersList);
        
        //expect wiremock to register a PATCH to NPWD
        // var requests = await wireMockFixture.WireMockAdminApi.GetRequestAsync()
    }
}