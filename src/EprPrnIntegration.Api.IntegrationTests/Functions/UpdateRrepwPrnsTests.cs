using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateRrepwPrnsTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsPrnToBackendApi()
    {
        var prnNumber = "PRN-TEST-001";
        var id = await RrepwApiStub.HasPrnUpdate(prnNumber);

        await PrnApiStub.AcceptsPrnV2();

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var entries = await PrnApiStub.GetV2Requests();

            entries.Count.Should().Be(1);

            var entry = entries[0];

            entry.Request.Body!.Should().Contain(prnNumber);

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

        var before = await LastUpdateService.GetLastUpdate("UpdateRrepwPrns") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var after = await LastUpdateService.GetLastUpdate("UpdateRrepwPrns");

            after.Should().BeAfter(before);
        });
    }

    [Fact]
    public async Task WhenRrepwApiHasNoData_LastUpdateStaysUntouched()
    {
        await RrepwApiStub.HasNoPrns();

        var before = await LastUpdateService.GetLastUpdate("UpdateRrepwPrns") ?? DateTime.MinValue;

        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrns);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await RrepwApiStub.GetPrnRequests();
            requests.Count.Should().Be(1);

            var after = await LastUpdateService.GetLastUpdate("UpdateRrepwPrns");
            after.Should().Be(before);
        });
    }
}
