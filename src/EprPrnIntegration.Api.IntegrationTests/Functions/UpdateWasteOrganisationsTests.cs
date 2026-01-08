using System.Net;
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

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, 2)]
    [InlineData(HttpStatusCode.RequestTimeout, 1)]
    [InlineData(HttpStatusCode.InternalServerError, 1)]
    [InlineData(HttpStatusCode.BadGateway, 1)]
    [InlineData(HttpStatusCode.TooManyRequests, 1)]
    [InlineData(HttpStatusCode.GatewayTimeout, 1)]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_RetriesAndEventuallySucceedsAndUpdatesLastUpdated(
        HttpStatusCode failureResponse,
        int failureCount
    )
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme-transient");

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            id,
            failureCount,
            failureResponse
        );

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
                .Be(
                    failureCount + 1,
                    $"Should be {failureCount} failures in a row then a success,so {failureCount + 1} requests"
                );
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain("acme-transient");
            mostRecentUpdate.Response.StatusCode.Should().Be(202);

            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
            after.Should().BeAfter(before);
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task WhenWasteOrganisationsApiHasTransientFailure_RetriesButGivesUpAfter3Retries(
        HttpStatusCode failureResponse
    )
    {
        var id = await CommonDataApiStub.HasV2UpdateFor("acme-transient");

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            id,
            4,
            failureResponse
        );

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
                .Be(4, "Should be an initial request then 3 retries, so four in total");
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain("acme-transient");
            mostRecentUpdate.Response.StatusCode.Should().Be((int)failureResponse);

            var after = await LastUpdateService.GetLastUpdate("UpdateWasteOrganisations");
            after.Should().NotBeAfter(before);
        });
    }
}
