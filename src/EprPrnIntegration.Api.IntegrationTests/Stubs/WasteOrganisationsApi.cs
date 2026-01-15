using System.Net;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class WasteOrganisationsApi(WireMockContext wireMock)
{
    public async Task AcceptsOrganisation(string id)
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingPut().WithPath($"/organisations/{id}/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task WithOrganisation(Guid id, string type)
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingGet().WithPath($"/organisations/{id}/"))
                .WithResponse(response =>
                    response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBodyAsJson(
                            new WoApiOrganisation
                            {
                                Id = id,
                                Address = new WoApiAddress(),
                                Registration = new WoApiRegistration
                                {
                                    RegistrationYear = 2024,
                                    Status = WoApiOrganisationStatus.Registered,
                                    Type = type,
                                },
                            }
                        )
                )
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task WithOrganisationsEndpointWIthNonTransientFailure(
        string id,
        HttpStatusCode failureResponse = HttpStatusCode.BadRequest
    )
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingPut().WithPath($"/organisations/{id}/"))
                .WithResponse(response => response.WithStatusCode(failureResponse))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task WithOrganisationsEndpointRecoveringFromTransientFailures(
        string id,
        int failureCount,
        HttpStatusCode failureResponse = HttpStatusCode.ServiceUnavailable
    )
    {
        var scenarioName = "WasteOrgTransientFailure-" + Guid.NewGuid();

        await wireMock.WithEndpointRecoveringFromTransientFailures(
            request =>
                request
                    .UsingPut()
                    .WithPath($"/organisations/{id}/")
                    .WithHeader("Authorization", "Bearer *"),
            response => response.WithStatusCode(HttpStatusCode.Accepted),
            response => response.WithStatusCode(failureResponse),
            failureCount
        );
    }

    public async Task<IList<LogEntryModel>> GetOrganisationRequests(string id)
    {
        var requestsModel = new RequestModel { Methods = ["PUT"], Path = $"/organisations/{id}/" };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }
}
