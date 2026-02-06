using EprPrnIntegration.Common.Configuration;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class FetchNpwdIssuedPrnsFunctionIntegrationTest : IntegrationTestBase
{
    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsIssuedPrnsToPrnService()
    {
        await Task.WhenAll(
            NpwdApiStub.HasIssuedPrns("ACC123456"),
            AccountApiStub.ValidatesIssuedEpr(),
            PrnApiStub.AcceptsPrnDetails(),
            AccountApiStub.HasPersonEmailForEpr()
        );

        await FunctionContext.Invoke(
            FunctionName.FetchNpwdIssuedPrnsFunction
        );

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await PrnApiStub.GetDetailRequests();

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("ACC123456"));
        });
    }
}
