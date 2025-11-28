using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateWasteOrganisationsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedWasteOrganisationToNPWD()
    {
        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);

        var now = DateTime.UtcNow;
        var lastUpdate = await LastUpdateService.GetLastUpdate("update-waste-organisations");

        lastUpdate.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
    }
}
