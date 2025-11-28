using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateWasteOrganisationsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedWasteOrganisationToNPWD()
    {
        var lastUpdateBefore = (await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations")) ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);
        
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var lastUpdateAfter = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");

            lastUpdateAfter.Should().BeAfter(lastUpdateBefore);
        });
    }
}
