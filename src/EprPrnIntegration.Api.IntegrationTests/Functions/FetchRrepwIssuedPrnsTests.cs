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
        await RrepwApiStub.HasPrnUpdates([prnNumber]);

        await PrnApiStub.AcceptsPrnV2();

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
        await RrepwApiStub.HasPrnUpdates(["PRN-TEST-002"]);

        await PrnApiStub.AcceptsPrnV2();

        var before =
            await LastUpdateService.GetLastUpdate("FetchRrepwIssuedPrns") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var after = await LastUpdateService.GetLastUpdate("FetchRrepwIssuedPrns");

            after.Should().BeAfter(before);
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

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, 2)]
    [InlineData(HttpStatusCode.RequestTimeout, 1)]
    [InlineData(HttpStatusCode.InternalServerError, 1)]
    [InlineData(HttpStatusCode.BadGateway, 1)]
    [InlineData(HttpStatusCode.TooManyRequests, 1)]
    [InlineData(HttpStatusCode.GatewayTimeout, 1)]
    public async Task WhenRrepwApiHasTransientFailure_RetriesAndEventuallySendsDataToCommonPrnApi(
        HttpStatusCode failureResponse,
        int failureCount
    )
    {
        var prnNumber = "PRN-TEST-001";
        await RrepwApiStub.HasPrnUpdatesWithTransientFailures(
            [prnNumber],
            failureResponse,
            failureCount
        );
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns)
            ?? DateTime.MinValue;
        await PrnApiStub.AcceptsPrnV2();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().Be(failureCount + 1);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)failureResponse);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var after = await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
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
    public async Task WhenRrepwApiHasTransientFailure_RetriesAndGivesUpAfter3Retries(
        HttpStatusCode failureResponse
    )
    {
        var prnNumber = "PRN-TEST-001";
        await RrepwApiStub.HasPrnUpdatesWithTransientFailures([prnNumber], failureResponse, 4);
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns)
            ?? DateTime.MinValue;
        await PrnApiStub.AcceptsPrnV2();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await RrepwApiStub.GetPrnRequests();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)failureResponse);

            var after = await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
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
    public async Task WhenCommonApiHasTransientFailure_RetriesAndEventuallySucceedsAndUpdatesLastUpdated(
        HttpStatusCode failureResponse,
        int failureCount
    )
    {
        var prnNumber = "PRN-TEST-001";
        await RrepwApiStub.HasPrnUpdates([prnNumber]);
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns)
            ?? DateTime.MinValue;
        await PrnApiStub.AcceptsPrnV2WithTransientFailures(failureResponse, failureCount);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(failureCount + 1);
            for (int i = 0; i < entries.Count - 1; i++)
                entries[i].Response.StatusCode.Should().Be((int)failureResponse);
            entries.Last().Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);
            var after = await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
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
    public async Task WhenCommonApiHasTransientFailure_RetriesAndFailsAfter3Retries(
        HttpStatusCode failureResponse
    )
    {
        var prnNumber = "PRN-TEST-001";
        await RrepwApiStub.HasPrnUpdates([prnNumber]);
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns)
            ?? DateTime.MinValue;
        await PrnApiStub.AcceptsPrnV2WithTransientFailures(failureResponse, 4);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(4);
            for (int i = 0; i < entries.Count; i++)
                entries[i].Response.StatusCode.Should().Be((int)failureResponse);

            var after = await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
            after.Should().NotBeAfter(before);
        });
    }

    [Fact]
    public async Task WhenCommonApiHasNonTransientFailure_ContinuesWithNextPrn()
    {
        var prnNumbers = new string[] { "PRN-TEST-001", "PRN-TEST-002" };
        await RrepwApiStub.HasPrnUpdates(prnNumbers);
        var before =
            await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns)
            ?? DateTime.MinValue;
        await PrnApiStub.AcceptsPrnV2WithNonTransientFailure(prnNumbers[0]);
        await PrnApiStub.AcceptsPrnV2ForId(prnNumbers[1]);

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetPrnDetailsUpdateV2();

            entries.Count.Should().Be(2);
            entries[0].Response.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
            entries[1].Response.StatusCode.Should().Be((int)HttpStatusCode.Accepted);

            var after = await LastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
            after.Should().BeAfter(before);
        });
    }
}
