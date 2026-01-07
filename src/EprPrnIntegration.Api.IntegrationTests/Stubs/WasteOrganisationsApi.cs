using System.Net;
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
                .WithRequest(request =>
                    request
                        .UsingPut()
                        .WithPath($"/organisations/{id}/")
                        .WithHeader("Authorization", "Bearer *")
                )
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task WithOrganisationsEndpointRecoveringFromTransientFailures(
        string id,
        int failureCount
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
            failureCount
        );
    }

    public async Task<IList<LogEntryModel>> GetOrganisationRequests(string id)
    {
        var requestsModel = new RequestModel { Methods = ["PUT"], Path = $"/organisations/{id}/" };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }
}
