using System.Net;
using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Mappers;
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

        await AzureFunctionInvokerContext.InvokeAzureFunction(
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

        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );

            after.Should().BeAfter(before);
        });
    }

    [Theory()]
    [InlineData(HttpStatusCode.ServiceUnavailable, 2)]
    [InlineData(HttpStatusCode.RequestTimeout, 1)]
    [InlineData(HttpStatusCode.InternalServerError, 1)]
    [InlineData(HttpStatusCode.BadGateway, 1)]
    [InlineData(HttpStatusCode.TooManyRequests, 1)]
    [InlineData(HttpStatusCode.GatewayTimeout, 1)]
    public async Task WhenCommonDataApiHasTransientFailure_RetriesAndEventuallySendsDataToWasteOrganisationsApi(
        HttpStatusCode failureResponse,
        int failureCount
    )
    {
        var id = await CommonDataApiStub.HasV2UpdateWithTransientFailures(
            failureResponse,
            failureCount
        );

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;
        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await CommonDataApiStub.GetUpdatedProducersRequests();

            entries.Count.Should().Be(failureCount + 1);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)failureResponse);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().BeGreaterThan(0);
            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );
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
    public async Task WhenCommonDataApiHasTransientFailure_RetriesAndGivesUpAfter3Retries(
        HttpStatusCode failureResponse
    )
    {
        var id = await CommonDataApiStub.HasV2UpdateWithTransientFailures(failureResponse, 4);
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;
        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.AcceptsOrganisation(id);

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await CommonDataApiStub.GetUpdatedProducersRequests();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)failureResponse);

            entries = await WasteOrganisationsApiStub.GetOrganisationRequests(id);

            entries.Count.Should().Be(0);
            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );
            after.Should().NotBeAfter(before);
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
        var ids = await CommonDataApiStub.HasV2UpdateFor();

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            ids[0],
            failureCount,
            failureResponse
        );

        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
            FunctionName.UpdateWasteOrganisations
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await WasteOrganisationsApiStub.GetOrganisationRequests(ids[0]);

            entries
                .Count.Should()
                .Be(
                    failureCount + 1,
                    $"Should be {failureCount} failures in a row then a success,so {failureCount + 1} requests"
                );
            var mostRecentUpdate = entries.Last();
            mostRecentUpdate.Request.Body!.Should().Contain(ids[0] + "_name");
            mostRecentUpdate.Response.StatusCode.Should().Be(202);

            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );
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
        var ids = await CommonDataApiStub.HasV2UpdateFor();

        await CognitoApiStub.SetupOAuthToken();
        await WasteOrganisationsApiStub.WithOrganisationsEndpointRecoveringFromTransientFailures(
            ids[0],
            4,
            failureResponse
        );

        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
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
            mostRecentUpdate.Response.StatusCode.Should().Be((int)failureResponse);

            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );
            after.Should().NotBeAfter(before);
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
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
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

            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );
            after.Should().NotBeAfter(before);

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
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.UpdateWasteOrganisations)
            ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(
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

            var after = await LastUpdateService.GetLastUpdate(
                FunctionName.UpdateWasteOrganisations
            );
            after.Should().BeAfter(before);
        });
    }
}
