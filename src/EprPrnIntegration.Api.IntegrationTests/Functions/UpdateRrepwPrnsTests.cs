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

    private List<PrnUpdateStatus> CreateAcceptedPrns(int count = 3)
    {
        return _fixture
            .Build<PrnUpdateStatus>()
            .With(p => p.PrnStatusId, (int)EprnStatus.ACCEPTED)
            .CreateMany(count)
            .ToList();
    }

    private List<PrnUpdateStatus> CreateRejectedPrns(int count = 3)
    {
        return _fixture
            .Build<PrnUpdateStatus>()
            .With(p => p.PrnStatusId, (int)EprnStatus.REJECTED)
            .CreateMany(count)
            .ToList();
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsAcceptedPrnToRrepw()
    {
        var payload = CreateAcceptedPrns();
        // Arrange: Set up the PRN backend to return an accepted PRN
        // PrnStatusId = 1 corresponds to ACCEPTED status
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnAccept();

        // Act: Invoke the function
        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        // Assert: Verify the accept request was sent to RREPW
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await RrepwApiStub.GetPrnAcceptRequests();

            foreach (var prnUpdate in payload)
            {
                var request = requests.FirstOrDefault(r =>
                    r.Request.Path.Contains(prnUpdate.PrnNumber)
                );
                request.Should().NotBeNull();
                var jsonDocument = JsonDocument.Parse(request.Request.Body!);

                jsonDocument
                    .RootElement.GetProperty("acceptedAt")
                    .GetDateTime()
                    .Should()
                    .Be(prnUpdate.StatusDate);

                request.Response.StatusCode.Should().Be(200);
            }
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsRejectedPrnToRrepw()
    {
        // Arrange: Set up the PRN backend to return a rejected PRN
        // PrnStatusId = 2 corresponds to REJECTED status
        var payload = CreateRejectedPrns();
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnReject();

        // Act: Invoke the function
        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        // Assert: Verify the reject request was sent to RREPW
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await RrepwApiStub.GetPrnRejectRequests();

            foreach (var prnUpdate in payload)
            {
                var request = requests.FirstOrDefault(r =>
                    r.Request.Path.Contains(prnUpdate.PrnNumber)
                );
                request.Should().NotBeNull();
                var jsonDocument = JsonDocument.Parse(request.Request.Body!);

                jsonDocument
                    .RootElement.GetProperty("rejectedAt")
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
        // Set up paginated responses - 2 pages with 2 items each
        await RrepwApiStub.HasPrnUpdates(
            ["PRN-PAGE1-001", "PRN-PAGE1-002"],
            cursor: null,
            nextCursor: "cursor-page-2"
        );

        await RrepwApiStub.HasPrnUpdates(
            ["PRN-PAGE2-001", "PRN-PAGE2-002"],
            cursor: "cursor-page-2",
            nextCursor: null
        );

        await PrnApiStub.AcceptsPrnV2();

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
    public async Task PrnAccept_WhenCommonApiHasTransientFailure_RetriesAndEventuallySendsDataToRrepwPrnApi()
    {
        var payload = CreateAcceptedPrns();
        await PrnApiStub.HasUpdatedPrnsWithTransientFailures(
            payload,
            HttpStatusCode.ServiceUnavailable,
            1
        );
        await RrepwApiStub.AcceptsPrnAccept();
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await AfterShouldBeAfterBefore(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenCommonApiHasTransientFailure_RetriesAndGivesUpAfter3Retries()
    {
        var payload = CreateAcceptedPrns();
        await PrnApiStub.HasUpdatedPrnsWithTransientFailures(
            payload,
            HttpStatusCode.ServiceUnavailable,
            1
        );
        await RrepwApiStub.AcceptsPrnAccept();
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);

            await AfterShouldNotBeAfterBefore(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasTransientFailure_RetriesAndEventuallySucceeds()
    {
        var payload = CreateAcceptedPrns();
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnAcceptWithFailures(HttpStatusCode.ServiceUnavailable, 1);
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnAcceptRequests();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await AfterShouldBeAfterBefore(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasTransientFailure_RetriesAndFailsAfter3Retries()
    {
        var payload = CreateAcceptedPrns();
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnAcceptWithFailures(HttpStatusCode.ServiceUnavailable, 4);
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnAcceptRequests();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await AfterShouldNotBeAfterBefore(before, FunctionName.UpdateRrepwPrns);
        });
    }

    [Fact]
    public async Task PrnAccept_WhenRrepwApiHasNonTransientFailure_ContinuesWithNextPrn()
    {
        var payload = CreateAcceptedPrns();
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnAcceptWithFailures(HttpStatusCode.BadRequest, 1);
        var before = await GetLastUpdate(FunctionName.UpdateRrepwPrns);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnAcceptRequests();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            await AfterShouldBeAfterBefore(before, FunctionName.UpdateRrepwPrns);
        });
    }
}
