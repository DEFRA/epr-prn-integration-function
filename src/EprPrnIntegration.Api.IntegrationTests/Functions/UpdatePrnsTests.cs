using EprPrnIntegration.Common.Configuration;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdatePrnsListTests : IntegrationTestBase
{
    [Theory]
    [InlineData(null, "2025")]
    [InlineData("2026", "2026")]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD(
        string? obligationYear,
        string expectedObligationYear
    )
    {
        await FunctionContext.Invoke(FunctionName.UpdatePrnsList, async () =>
        {
            await Task.WhenAll(
                PrnApiStub.HasUpdateFor("PRN001234567", obligationYear),
                PrnApiStub.AcceptsSyncStatus(),
                NpwdApiStub.AcceptsPrnPatch()
            );
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await NpwdApiStub.GetPrnPatchRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
            Assert.Contains(
                requests,
                entry =>
                    entry.Request.Body!.Contains($"\"ObligationYear\":\"{expectedObligationYear}\"")
            );
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await PrnApiStub.GetUpdateSyncStatusRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });
    }
}
