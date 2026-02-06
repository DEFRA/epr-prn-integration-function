using System.Net;
using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateWasteOrganisationsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedWasteOrganisationToApi()
    {
        var ids = await CommonDataApiStub.HasV2UpdateFor();

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(ids[0]);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[0]);

            entries.Count.Should().Be(1);

            var entry = entries[0];

            entry.Request.Body!.Should().Contain(ids[0] + "_name");

            var jsonDocument = JsonDocument.Parse(entry.Request.Body!);

            jsonDocument
                .RootElement.GetProperty("registration")
                .GetProperty("type")
                .GetString()
                .Should()
                .Be(WoApiOrganisationType.ComplianceScheme);

            jsonDocument
                .RootElement.GetProperty("registration")
                .GetProperty("status")
                .GetString()
                .Should()
                .Be(WoApiOrganisationStatus.Registered);

            entry.Response.StatusCode.Should().Be(202);
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_With_UpdatesFound_UpdatesLastUpdatedTimestamp()
    {
        var ids = await CommonDataApiStub.HasV2UpdateFor();

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(ids[0]);

        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateWasteOrganisations);
        });
    }

    [Fact]
    public async Task WhenCommonDataApiHasTransientFailure_RetriesAndEventuallySendsDataToWasteOrganisationsApi()
    {
        var id = await CommonDataApiStub.HasV2UpdateWithTransientFailures(
            HttpStatusCode.ServiceUnavailable,
            2
        );

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);
        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);
        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await CommonDataApiStub.GetUpdatedProducersRequests();

            entries.Count.Should().Be(3);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().BeGreaterThan(0);
            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateWasteOrganisations);
        });
    }

    [Fact]
    public async Task WhenCommonDataApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        var id = await CommonDataApiStub.HasV2UpdateWithTransientFailures(
            HttpStatusCode.ServiceUnavailable,
            4
        );
        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);
        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await CommonDataApiStub.GetUpdatedProducersRequests();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);

            entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().Be(0);
            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateWasteOrganisations);
        });
    }

    [Fact]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_RetriesAndEventuallySucceedsAndUpdatesLastUpdated()
    {
        var ids = await CommonDataApiStub.HasV2UpdateFor();

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            ids[0],
            2,
            HttpStatusCode.ServiceUnavailable
        );

        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[0]);

            entries
                .Count.Should()
                .Be(3, $"Should be 2 failures in a row then a success,so 3 requests");
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain(ids[0] + "_name");
            mostRecentUpdate.Response.StatusCode.Should().Be(202);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateWasteOrganisations);
        });
    }

    [Fact]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_RetriesButGivesUpAfter3Retries()
    {
        var ids = await CommonDataApiStub.HasV2UpdateFor();

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            ids[0],
            4,
            HttpStatusCode.ServiceUnavailable
        );

        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[0]);

            entries
                .Count.Should()
                .Be(4, "Should be an initial request then 3 retries, so four in total");
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain(ids[0] + "_name");
            mostRecentUpdate
                .Response.StatusCode.Should()
                .Be((int)HttpStatusCode.ServiceUnavailable);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateWasteOrganisations);
        });
    }

    [Fact]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_DoesNotContinueWithNextOrganisation()
    {
        var ids = await CommonDataApiStub.HasV2UpdateFor(2);

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            ids[0],
            4,
            HttpStatusCode.ServiceUnavailable
        );
        await WasteOrganisationsApiStub.AcceptsOrganisation(ids[1]);
        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[0]);

            entries
                .Count.Should()
                .Be(4, "Should be an initial request then 3 retries, so four in total");
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain(ids[0] + "_name");
            mostRecentUpdate
                .Response.StatusCode.Should()
                .Be((int)HttpStatusCode.ServiceUnavailable);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateWasteOrganisations);

            entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[1]);
            entries.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task WhenWasteOrganisationsApiHasNonTransientFailure_ContinuesWithNextOrganisation()
    {
        var ids = await CommonDataApiStub.HasV2UpdateFor(2);

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointWIthNonTransientFailure(ids[0]);
        await WasteOrganisationsApiStub.AcceptsOrganisation(ids[1]);
        var before = await GetLastUpdate(FunctionName.UpdateWasteOrganisations);

        await FunctionContext.Invoke(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[0]);

            entries.Count.Should().Be(1, "Should only be one failed attempt");
            entries[0].Request.Body!.Should().Contain(ids[0] + "_name");
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[1]);
            entries.Count.Should().Be(1);
            entries[0].Request.Body!.Should().Contain(ids[1] + "_name");
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateWasteOrganisations);
        });
    }
}
