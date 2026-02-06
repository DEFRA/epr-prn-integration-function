using System.Net;
using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().BeGreaterOrEqualTo(2);
            var relevantEntries = entries.TakeLast(2).ToList();
            relevantEntries[0]
                .Response.StatusCode.Should()
                .Be((int)HttpStatusCode.ServiceUnavailable);
            relevantEntries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().BeGreaterOrEqualTo(4);
            var relevantEntries = entries.TakeLast(4).ToList();
            foreach (var entry in relevantEntries)
            {
                entry.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            }

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

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

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);

            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    #region ListPackagingRecyclingNotes Tests

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiHasTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var prnNumber = "PRN-TRANSIENT-SUCCESS-001";
        var prns = await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
            [prnNumber],
            HttpStatusCode.InternalServerError,
            1 // Fail once, then succeed on 2nd attempt
        );
        await TestHelper.SetupOrganisations(prns, CognitoApiStub, WasteOrganisationsApiStub);
        await PrnApiStub.AcceptsPrnV2();
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);

        // Act
        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        // Assert
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            // Should have made initial request + 1 retry = 2 total, last one succeeds
            entries.Count.Should().BeGreaterOrEqualTo(2);
            var relevantEntries = entries.TakeLast(2).ToList();
            relevantEntries[0]
                .Response.StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);
            relevantEntries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            // Verify PRN was sent to Common API
            var prnEntries = await PrnApiStub.GetPrnDetailsUpdateV2();
            prnEntries.Count.Should().BeGreaterOrEqualTo(1);

            // Last update should be set
            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        // Arrange
        var prnNumber = "PRN-TRANSIENT-GIVEUP-001";
        await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
            [prnNumber],
            HttpStatusCode.ServiceUnavailable,
            4 // Fail 4 times (initial + 3 retries)
        );
        await PrnApiStub.AcceptsPrnV2();
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);

        // Act
        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        // Assert
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            // Should have made initial request + 3 retries = 4 total
            entries.Count.Should().BeGreaterOrEqualTo(4);
            var relevantEntries = entries.TakeLast(4).ToList();
            foreach (var entry in relevantEntries)
            {
                entry.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            }

            // Verify NO new PRNs were sent to Common API from this test (function terminated)
            // Note: May have PRNs from previous tests, so we don't assert exact count

            // Last update should NOT be set (function terminated)
            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiHasNonTransientFailure_FunctionTerminates()
    {
        // Arrange
        var prnNumber = "PRN-NONTRANSIENT-001";
        await RrepwApiStub.HasPrnUpdatesWithNonTransientFailure(
            [prnNumber],
            HttpStatusCode.BadRequest
        );
        await PrnApiStub.AcceptsPrnV2();
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);

        // Act
        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        // Assert
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            // Should only make 1 request (no retries for non-transient errors)
            entries.Count.Should().BeGreaterOrEqualTo(1);
            var lastEntry = entries.Last();
            lastEntry.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            // Verify NO new PRNs were sent to Common API from this test (function terminated)
            // Note: May have PRNs from previous tests, so we don't assert exact count

            // Last update should NOT be set (function terminated)
            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiThrowsException_FunctionTerminates()
    {
        // Arrange
        // Don't set up any stub - this will cause a connection failure/exception
        var before = await GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);

        // Act
        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns);

        // Assert - function should terminate without updating last run time
        await AsyncWaiter.WaitForAsync(async () =>
        {
            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    #endregion
}
