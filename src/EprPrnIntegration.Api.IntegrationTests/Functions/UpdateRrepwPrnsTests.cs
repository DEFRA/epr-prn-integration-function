using System.Net;
using System.Text.Json;
using AutoFixture;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateRrepwPrnsTests : IntegrationTestBase
{
    private readonly Fixture _fixture = new();

    private List<PrnUpdateStatus> CreatePrns(EprnStatus eprnStatus, int count = 3)
    {
        return _fixture
            .Build<PrnUpdateStatus>()
            .With(p => p.PrnStatusId, (int)eprnStatus)
            .CreateMany(count)
            .ToList();
    }

    [Theory]
    [InlineData(EprnStatus.ACCEPTED)]
    [InlineData(EprnStatus.REJECTED)]
    public async Task WhenAzureFunctionIsInvoked_SendsAcceptedPrnToRrepw(EprnStatus eprnStatus)
    {
        var payload = CreatePrns(eprnStatus);
        
        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            await PrnApiStub.HasModifiedPrns(payload);
            await RrepwApiStub.AcceptsPrn(eprnStatus);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await RrepwApiStub.GetUpdatePrnRequests(eprnStatus);

            foreach (var prnUpdate in payload)
            {
                var request = requests.FirstOrDefault(r =>
                    r.Request.Path.Contains(prnUpdate.PrnNumber)
                );
                request.Should().NotBeNull();
                var jsonDocument = JsonDocument.Parse(request.Request.Body!);

                jsonDocument
                    .RootElement.GetProperty(
                        eprnStatus == EprnStatus.ACCEPTED ? "acceptedAt" : "rejectedAt"
                    )
                    .GetDateTime()
                    .Should()
                    .Be(prnUpdate.StatusDate);

                request.Response.StatusCode.Should().Be(200);
            }
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_WithPaginatedData_SendsAllPrnsToBackendApi()
    {
        // Why is this function call here?
        await FunctionContext.Invoke(FunctionName.FetchRrepwIssuedPrns, async () =>
        {
            var prns = await RrepwApiStub.HasPrnUpdates(
                ["PRN-PAGE1-001", "PRN-PAGE1-002"],
                cursor: null,
                nextCursor: "cursor-page-2"
            );

            prns =
            [
                .. prns,
                .. await RrepwApiStub.HasPrnUpdates(
                    ["PRN-PAGE2-001", "PRN-PAGE2-002"],
                    cursor: "cursor-page-2",
                    nextCursor: null
                ),
            ];
            await SetupOrganisations(prns);
            await PrnApiStub.AcceptsPrnV2();
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
    public async Task PrnAccept_WhenCommonApiHasTransientFailure_RetriesAndEventuallySendsDataToRrepwPrnApi()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.REJECTED, 1);
            await PrnApiStub.HasModifiedPrnsWithTransientFailures(
                payload,
                HttpStatusCode.ServiceUnavailable,
                1
            );
            await RrepwApiStub.AcceptsPrn(EprnStatus.REJECTED);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.FindModifiedPrnsRequest();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenCommonApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.REJECTED, 1);
            await PrnApiStub.HasModifiedPrnsWithTransientFailures(
                payload,
                HttpStatusCode.ServiceUnavailable,
                4
            );
            await RrepwApiStub.AcceptsPrn(EprnStatus.REJECTED);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.FindModifiedPrnsRequest();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasTransientFailure_RetriesAndEventuallySucceeds()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.ACCEPTED, 1);
            await PrnApiStub.HasModifiedPrns(payload);
            await RrepwApiStub.AcceptsPrnWithFailures(
                EprnStatus.ACCEPTED,
                HttpStatusCode.ServiceUnavailable,
                1
            );
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.ACCEPTED);

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasTransientFailure_RetriesAndFailsAfter3Retries()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.REJECTED, 1);
            await PrnApiStub.HasModifiedPrns(payload);
            await RrepwApiStub.AcceptsPrnWithFailures(
                EprnStatus.REJECTED,
                HttpStatusCode.ServiceUnavailable,
                4
            );
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.REJECTED);

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task PrnAccept_WhenRrepwApiHasStatusThatShouldNotContinue_FailAndNotContinue(HttpStatusCode statusCode)
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.REJECTED, 1);
            await PrnApiStub.HasModifiedPrns(payload);
            await RrepwApiStub.AcceptsPrnWithFailures(
                EprnStatus.REJECTED,
                statusCode,
                1
            );
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.REJECTED);

            entries.Count.Should().Be(1);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)statusCode);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasNonTransientFailure_ContinuesWithNextPrn()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.ACCEPTED, 2);
            await PrnApiStub.HasModifiedPrns(payload);
            await RrepwApiStub.AcceptsPrnWithFailures(
                EprnStatus.ACCEPTED,
                HttpStatusCode.BadRequest,
                1
            );
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.ACCEPTED);

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasError409_ContinuesWithNextPrn()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.ACCEPTED, 2);
            await PrnApiStub.HasModifiedPrns(payload);
            await RrepwApiStub.AcceptsPrnWithFailures(EprnStatus.ACCEPTED, HttpStatusCode.Conflict, 1);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.ACCEPTED);

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task GetUpdatedRrepwPrnsAsync_WhenCommonApiHasTransientFailure_RetriesAndSucceeds()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.ACCEPTED, 1);
            await PrnApiStub.HasModifiedPrnsWithTransientFailures(
                payload,
                HttpStatusCode.InternalServerError,
                1
            );
            await RrepwApiStub.AcceptsPrn(EprnStatus.ACCEPTED);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.FindModifiedPrnsRequest();

            entries.Count.Should().BeGreaterOrEqualTo(2);
            var relevantEntries = entries.TakeLast(2).ToList();
            relevantEntries[0]
                .Response.StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);
            relevantEntries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            // Verify PRN was sent to RREPW
            var rrepwEntries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.ACCEPTED);
            rrepwEntries.Count.Should().BeGreaterOrEqualTo(1);

            await LastUpdateShouldHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task GetUpdatedRrepwPrnsAsync_WhenCommonApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.ACCEPTED, 1);
            await PrnApiStub.HasModifiedPrnsWithTransientFailures(
                payload,
                HttpStatusCode.ServiceUnavailable,
                4
            );
            await RrepwApiStub.AcceptsPrn(EprnStatus.ACCEPTED);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.FindModifiedPrnsRequest();

            entries.Count.Should().Be(4);
            foreach (var entry in entries)
            {
                entry.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            }

            // Verify NO PRNs were sent to RREPW (function terminated)
            var rrepwEntries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.ACCEPTED);
            rrepwEntries.Count.Should().Be(0);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task GetUpdatedRrepwPrnsAsync_WhenCommonApiHasNonTransientFailure_FunctionTerminates()
    {
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns, async () =>
        {
            var payload = CreatePrns(EprnStatus.ACCEPTED, 1);
            await PrnApiStub.HasModifiedPrnsWithNonTransientFailure(payload, HttpStatusCode.BadRequest);
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.FindModifiedPrnsRequest();

            entries.Count.Should().Be(1);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            // Verify NO PRNs were sent to RREPW (function terminated)
            var rrepwEntries = await RrepwApiStub.GetUpdatePrnRequests(EprnStatus.ACCEPTED);
            rrepwEntries.Count.Should().Be(0);

            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task GetUpdatedRrepwPrnsAsync_WhenCommonApiThrowsException_FunctionTerminates()
    {
        // Don't set up any stub - this will cause a connection failure/exception
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await FunctionContext.Invoke(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            await LastUpdateShouldNotHaveChanged(before, FunctionName.UpdateRrepwPrns);
        });
    }
}
