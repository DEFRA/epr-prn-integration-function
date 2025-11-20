using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class UpdatePrnsListFunctionIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private AzureFunctionInvokerContext _azureFunctionInvokerContext = null!;
    private WireMockContext _wireMockContext = null!;

    public async Task InitializeAsync()
    {
        _wireMockContext = new WireMockContext();
        await _wireMockContext.InitializeAsync();

        _azureFunctionInvokerContext = new AzureFunctionInvokerContext();
    }

    public async Task DisposeAsync()
    {
        await _wireMockContext.DisposeAsync();
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsUpdatedProducerToNPWD()
    {
        await Task.WhenAll(
            Given_PrnApiHasUpdateFor("PRN001234567"),
            Given_PrnApiAcceptsSyncStatus(),
            Given_NpwdAcceptsPrnPatch());

        await _azureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdatePrnsList);

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requestsModel = new RequestModel { Methods = ["PATCH"], Path = "/odata/PRNs" };
            var requests = await _wireMockContext.WireMockAdminApi.FindRequestsAsync(requestsModel);

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });

        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requestsModel = new RequestModel { Methods = ["POST"], Path = "/api/v1/prn/updatesyncstatus/" };
            var requests = await _wireMockContext.WireMockAdminApi.FindRequestsAsync(requestsModel);

            Assert.Contains(requests, entry => entry.Request.Body!.Contains("PRN001234567"));
        });
    }

    private async Task Given_NpwdAcceptsPrnPatch()
    {
        var mappingBuilder = _wireMockContext.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPatch().WithPath("/odata/PRNs"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    private async Task Given_PrnApiAcceptsSyncStatus()
    {
        var mappingBuilder = _wireMockContext.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPost().WithPath("/api/v1/prn/updatesyncstatus/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    private async Task Given_PrnApiHasUpdateFor(string evidenceNo)
    {
        var mappingBuilder = _wireMockContext.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/api/v1/prn/ModifiedPrnsByDate"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new[]
                {
                    new
                    {
                        evidenceNo,
                        evidenceStatusCode = "EV-ACCEP",
                        statusDate = "2025-01-15T10:30:00Z"
                    }
                }))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }
}