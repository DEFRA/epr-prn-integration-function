using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateWasteOrganisationsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedWasteOrganisationToApi()
    {
        var token = await CognitoApiStub.SetupOAuthTokenEndpoint();
        var id = await CommonDataApiStub.HasV2UpdateFor("acme");
        await WasteOrganisationsApiStub.AcceptsOrganisation(id, token);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);
        
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().Be(1);

            var entry = entries[0];

            entry.Request.Body!.Should().Contain("acme");

            var jsonDocument = JsonDocument.Parse(entry.Request.Body!);

            jsonDocument.RootElement
                .GetProperty("registration")
                .GetProperty("type")
                .GetString().Should().Be("COMPLIANCE_SCHEME");
            
            jsonDocument.RootElement
                .GetProperty("registration")
                .GetProperty("status")
                .GetString().Should().Be("REGISTERED");
        });
    }
    
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_With_UpdatesFound_UpdatesLastUpdatedTimestamp()
    {
        var token = await CognitoApiStub.SetupOAuthTokenEndpoint();
        var id = await CommonDataApiStub.HasV2UpdateFor("acme");
        await WasteOrganisationsApiStub.AcceptsOrganisation(id, token);

        var before = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");

            after.Should().BeAfter(before);
        });
    }

    [Fact]
    public async Task WhenCommonDataApiHasTransientFailure_RetriesAndEventuallySendsDataToWasteOrganisationsApi()
    {
        var id = await CommonDataApiStub.HasV2UpdateWithTransientFailures("acme-resilient");
        
        var token = await CognitoApiStub.SetupOAuthTokenEndpoint();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id, token);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().Be(1);
            entries[0].Request.Body!.Should().Contain("acme-resilient");
        });
    }

    [Fact]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_RetriesAndEventuallySucceedsAndUpdatesLastUpdated()
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme-transient");
        
        var token = await CognitoApiStub.SetupOAuthTokenEndpoint();
        await WasteOrganisationsApiStub.AcceptsOrganisationWithTransientFailures(id, token);

        var before = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().BeGreaterOrEqualTo(1, "request should eventually succeed after retry");
            entries[0].Request.Body!.Should().Contain("acme-transient");

            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
            after.Should().BeAfter(before);
        });
    }
    
    [Fact]
    public async Task WhenCommonDataApiHasNoData_LastUpdateStaysUntouched()
    {
        await CommonDataApiStub.HasNoUpdates();

        var before = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateWasteOrganisations);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await CommonDataApiStub.GetUpdatedProducersRequests();
            requests.Count.Should().Be(1);
            
            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
            after.Should().Be(before);
        });
    }
}
