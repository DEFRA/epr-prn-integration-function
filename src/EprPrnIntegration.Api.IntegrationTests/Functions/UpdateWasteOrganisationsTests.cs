using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateWasteOrganisationsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedWasteOrganisationToApi()
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme");

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().Be(1);

            var entry = entries[0];

            entry.Request.Body!.Should().Contain("acme");

            var jsonDocument = JsonDocument.Parse(entry.Request.Body!);

            jsonDocument
                .RootElement.GetProperty("registration")
                .GetProperty("type")
                .GetString()
                .Should()
                .Be("COMPLIANCE_SCHEME");

            jsonDocument
                .RootElement.GetProperty("registration")
                .GetProperty("status")
                .GetString()
                .Should()
                .Be("REGISTERED");

            entry.Response.StatusCode.Should().Be(202);
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_With_UpdatesFound_UpdatesLastUpdatedTimestamp()
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme");

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);

        var before =
            await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

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

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().Be(1);
            entries[0].Request.Body!.Should().Contain("acme-resilient");

            entries[0].Response.StatusCode.Should().Be(202);
        });
    }

    [Fact]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_RetriesAndEventuallySucceedsAndUpdatesLastUpdated()
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme-transient");

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisationWithTransientFailures(id);

        var before =
            await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries
                .Count.Should()
                .BeGreaterOrEqualTo(1, "request should eventually succeed after retry");
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain("acme-transient");
            mostRecentUpdate.Response.StatusCode.Should().Be(202);

            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
            after.Should().BeAfter(before);
        });
    }
}
