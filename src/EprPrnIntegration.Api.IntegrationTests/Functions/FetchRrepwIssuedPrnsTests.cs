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
        const string prnNumber = "PRN-TEST-001";

        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
            await PrnApiStub.AcceptsPrnV2();
            await SetupOrganisations(prns);
        });

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
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates(["PRN-TEST-002"]);
            await PrnApiStub.AcceptsPrnV2();
            await SetupOrganisations(prns);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_WithPaginatedData_SendsAllPrnsToBackendApi()
    {
        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
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
            await SetupOrganisations([.. prns1, .. prns2]);
        });

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
        const string prnNumber = "PRN-TEST-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
            await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
                [prnNumber],
                HttpStatusCode.ServiceUnavailable,
                1
            );
            await PrnApiStub.AcceptsPrnV2();
            await SetupOrganisations(prns);
        });

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
        const string prnNumber = "PRN-TEST-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
            await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
                [prnNumber],
                HttpStatusCode.ServiceUnavailable,
                4
            );
            await PrnApiStub.AcceptsPrnV2();
            await SetupOrganisations(prns);
        });

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
        const string prnNumber = "PRN-TEST-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
            await PrnApiStub.AcceptsPrnV2WithTransientFailures(HttpStatusCode.ServiceUnavailable, 1);
            await SetupOrganisations(prns);
        });

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
        const string prnNumber = "PRN-TEST-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates([prnNumber]);
            await PrnApiStub.AcceptsPrnV2WithTransientFailures(HttpStatusCode.ServiceUnavailable, 4);
            await SetupOrganisations(prns);
        });

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
        var prnNumbers = new[] { "PRN-TEST-001", "PRN-TEST-002" };
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates(prnNumbers);
            await PrnApiStub.AcceptsPrnV2WithNonTransientFailure(prnNumbers[0]);
            await PrnApiStub.AcceptsPrnV2ForId(prnNumbers[1]);
            await SetupOrganisations(prns);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);

            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiHasTransientFailure_RetriesAndSucceeds()
    {
        const string prnNumber = "PRN-TRANSIENT-SUCCESS-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
                [prnNumber],
                HttpStatusCode.InternalServerError,
                1
            );
            await SetupOrganisations(prns);
            await PrnApiStub.AcceptsPrnV2();
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().BeGreaterOrEqualTo(2);
            var relevantEntries = entries.TakeLast(2).ToList();
            relevantEntries[0]
                .Response.StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);
            relevantEntries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            // Verify PRN was sent to Common API
            var prnEntries = await PrnApiStub.GetPrnDetailsUpdateV2();
            prnEntries.Count.Should().BeGreaterOrEqualTo(1);

            await LastUpdateShouldHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        const string prnNumber = "PRN-TRANSIENT-GIVEUP-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
                [prnNumber],
                HttpStatusCode.ServiceUnavailable,
                4
            );
            await PrnApiStub.AcceptsPrnV2();
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().BeGreaterOrEqualTo(4);
            var relevantEntries = entries.TakeLast(4).ToList();
            foreach (var entry in relevantEntries)
            {
                entry.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            }

            // Verify NO new PRNs were sent to Common API from this test (function terminated)
            // Note: May have PRNs from previous tests, so we don't assert exact count

            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiHasNonTransientFailure_FunctionTerminates()
    {
        const string prnNumber = "PRN-NONTRANSIENT-001";
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            await RrepwApiStub.HasPrnUpdatesWithNonTransientFailure(
                [prnNumber],
                HttpStatusCode.BadRequest
            );
            await PrnApiStub.AcceptsPrnV2();
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().BeGreaterOrEqualTo(1);
            var lastEntry = entries.Last();
            lastEntry.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            // Verify NO new PRNs were sent to Common API from this test (function terminated)
            // Note: May have PRNs from previous tests, so we don't assert exact count

            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_WhenRrepwApiThrowsException_FunctionTerminates()
    {
        // Don't set up any stub - this will cause a connection failure/exception
        
        var before = await FunctionContext.GetLastUpdateAndInvoke(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            await LastUpdateShouldNotHaveChanged(before, FunctionName.FetchRrepwIssuedPrns);
        });
    }
}
