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
            builder.WithRequest(request => request.UsingPut().WithPath($"/organisations/{id}/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetOrganisationRequests(string id)
    {
        var requestsModel = new RequestModel { Methods = ["PUT"], Path = $"/organisations/{id}/" };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public async Task AcceptsOrganisationWithTransientFailures(string id)
    {
        var scenarioName = "WasteOrgTransientFailure-" + Guid.NewGuid();

        // First mapping: return 503 and transition to "Attempt1" state
        var failureMapping = wireMock.WireMockAdminApi.GetMappingBuilder();
        failureMapping.Given(builder =>
            builder.WithRequest(request => request.UsingPut().WithPath($"/organisations/{id}/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.ServiceUnavailable))
                .WithScenario(scenarioName)
                .WithSetStateTo("Attempt1")
        );
        var failureMappingStatus = await failureMapping.BuildAndPostAsync();
        Assert.NotNull(failureMappingStatus.Guid);

        // Second mapping: return 202 Accepted when in "Attempt1" state
        var successMapping = wireMock.WireMockAdminApi.GetMappingBuilder();
        successMapping.Given(builder =>
            builder.WithRequest(request => request.UsingPut().WithPath($"/organisations/{id}/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
                .WithScenario(scenarioName)
                .WithWhenStateIs("Attempt1")
        );
        var successMappingStatus = await successMapping.BuildAndPostAsync();
        Assert.NotNull(successMappingStatus.Guid);
    }
}