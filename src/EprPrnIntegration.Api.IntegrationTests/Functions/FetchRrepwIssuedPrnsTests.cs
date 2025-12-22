using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class FetchRrepwIssuedPrnsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsPrnToBackendApi()
    {
        var prnNumber = "PRN-TEST-001";
        await RrepwApiStub.HasPrnUpdate(prnNumber);

        await PrnApiStub.AcceptsPrnV2();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetV2Requests();

            entries.Count.Should().Be(1);

            var entry = entries[0];
            var jsonDocument = JsonDocument.Parse(entry.Request.Body!);

            jsonDocument.RootElement
                .GetProperty("prnNumber")
                .GetString().Should().Be(prnNumber);

            entry.Response.StatusCode.Should().Be(202);
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_WithPrnsFound_UpdatesLastUpdatedTimestamp()
    {
        var prnNumber = "PRN-TEST-002";
        await RrepwApiStub.HasPrnUpdate(prnNumber);

        await PrnApiStub.AcceptsPrnV2();

        var before = await LastUpdateService.GetLastUpdate("FetchRrepwIssuedPrns") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.FetchRrepwIssuedPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var after = await LastUpdateService.GetLastUpdate("FetchRrepwIssuedPrns");

            after.Should().BeAfter(before);
        });
    }
}
