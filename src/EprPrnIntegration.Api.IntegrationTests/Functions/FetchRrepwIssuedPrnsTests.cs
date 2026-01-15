using System.Net;
using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class FetchRrepwIssuedPrnsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsPrnToBackendApi()
    {
        var prnNumber = "PRN-TEST-001";
        var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);

        await PrnApiStub.AcceptsPrnV2();
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(1);

            var entry = entries[0];
            var jsonDocument = JsonDocument.Parse(entry.Request.Body!);

            jsonDocument.RootElement.GetProperty("prnNumber").GetString().Should().Be(prnNumber);

            entry.Response.StatusCode.Should().Be(202);
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_WithPrnsFound_UpdatesLastUpdatedTimestamp()
    {
        var prns = await RrepwApiStub.HasPrnUpdates(["PRN-TEST-002"]);

        await PrnApiStub.AcceptsPrnV2();
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_WithPaginatedData_SendsAllPrnsToBackendApi()
    {
        // Set up paginated responses - 2 pages with 2 items each
        var prns1 = await RrepwApiStub.HasPrnUpdates(
            ["PRN-PAGE1-001", "PRN-PAGE1-002"],
            cursor: null,
            nextCursor: "cursor-page-2"
        );

        var prns2 = await RrepwApiStub.HasPrnUpdates(
            ["PRN-PAGE2-001", "PRN-PAGE2-002"],
            cursor: "cursor-page-2",
            nextCursor: null
        );

        await PrnApiStub.AcceptsPrnV2();
        await TestHelper.SetupOrganisations(
            [.. prns1, .. prns2],
            CognitoApiStub,
            WasteOrganisationsApiStub
        );

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(4);

            var receivedPrnNumbers = entries
                .Select(entry =>
                {
                    var jsonDocument = JsonDocument.Parse(entry.Request.Body!);
                    return jsonDocument.RootElement.GetProperty("prnNumber").GetString();
                })
                .OrderBy(prn => prn)
                .ToList();

            var expectedPrnNumbers = new[]
            {
                "PRN-PAGE1-001",
                "PRN-PAGE1-002",
                "PRN-PAGE2-001",
                "PRN-PAGE2-002",
            };

            receivedPrnNumbers.Should().BeEquivalentTo(expectedPrnNumbers.OrderBy(prn => prn));
        });
    }

    [Fact]
    public async Task WhenRrepwApiHasTransientFailure_RetriesAndEventuallySendsDataToCommonPrnApi()
    {
        var prnNumber = "PRN-TEST-001";
        var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
        await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
            [prnNumber],
            HttpStatusCode.ServiceUnavailable,
            1
        );
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        await PrnApiStub.AcceptsPrnV2();
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().Be(2);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task WhenRrepwApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        var prnNumber = "PRN-TEST-001";
        var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
        await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
            [prnNumber],
            HttpStatusCode.ServiceUnavailable,
            4
        );
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        await PrnApiStub.AcceptsPrnV2();
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task WhenCommonApiHasTransientFailure_RetriesAndEventuallySucceedsAndUpdatesLastUpdated()
    {
        var prnNumber = "PRN-TEST-001";
        var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        await PrnApiStub.AcceptsPrnV2WithTransientFailures(HttpStatusCode.ServiceUnavailable, 1);
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);
            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task WhenCommonApiHasTransientFailure_RetriesAndFailsAfter3Retries()
    {
        var prnNumber = "PRN-TEST-001";
        var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        await PrnApiStub.AcceptsPrnV2WithTransientFailures(HttpStatusCode.ServiceUnavailable, 4);
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task WhenCommonApiHasNonTransientFailure_ContinuesWithNextPrn()
    {
        var prnNumbers = new string[] { "PRN-TEST-001", "PRN-TEST-002" };
        var prns = await RrepwApiStub.HasPrnUpdates(prnNumbers);
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        await PrnApiStub.AcceptsPrnV2WithNonTransientFailure(prnNumbers[0]);
        await PrnApiStub.AcceptsPrnV2ForId(prnNumbers[1]);
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);

            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }
}
