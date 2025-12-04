using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateWasteOrganisationsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedWasteOrganisationToApi()
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme");
        await WasteOrganisationsApiStub.AcceptsOrganisation(id); 
        
        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);
        
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);
            
            entries.Count.Should().Be(1);
            
            var entry = entries[0];

            entry.Request.Body!.Should().Contain("acme");
        });
    }
    
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_UpdatesLastUpdatedTimestamp()
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme");
        await WasteOrganisationsApiStub.AcceptsOrganisation(id); 
        
        var before = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);
        
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");

            after.Should().BeAfter(before);
        });
    }
}
