using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

[Collection("EPRFunctions")]
public class ProducerUpsertFunctionIntegrationTest : IntegrationTestBase, IAsyncLifetime
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
    public async Task Azure_function_sends_updated_producer_to_NPWD_via_PATCH()
    {
        var mappingBuilder = _wireMockContext.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/api/producer-details/get-updated-producers"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new[]
                {
                    new
                    {
                        organisationName = "Acme Manufacturing Ltd",
                        tradingName = "Acme Plastics",
                        organisationType = "Limited Company",
                        companiesHouseNumber = "12345678",
                        organisationId = "ORG-001",
                        addressLine1 = "123 Industrial Estate",
                        addressLine2 = "Unit 5",
                        town = "Manchester",
                        county = "Greater Manchester",
                        country = "England",
                        postcode = "M1 1AA",
                        peprid = "EPR-123456",
                        status = "Active",
                        businessCountry = "United Kingdom",
                        updatedDateTime = "2025-01-15T10:30:00Z"
                    }
                }))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);

        mappingBuilder = _wireMockContext.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPatch().WithPath("/odata/producers"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);

        await _azureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateProducersList);

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var requestsModel = new RequestModel { Methods = ["PATCH"], Path = "/odata/producers" };
                var requests = await _wireMockContext.WireMockAdminApi.FindRequestsAsync(requestsModel);

                return requests.Any(x => x.Request.Body.Contains("Acme Manufacturing Ltd"));
            })
        );
    }
}