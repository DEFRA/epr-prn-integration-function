using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class NpwdApi(WireMockContext wireMock)
{
    public async Task AcceptsProducerPatch()
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPatch().WithPath("/odata/producers"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsPrnPatch()
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPatch().WithPath("/odata/PRNs"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task HasIssuedPrns(string accreditationNo)
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/odata/PRNs"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(
                    new
                    {
                        value = new[]
                        {
                            new
                            {
                                AccreditationNo = accreditationNo,
                                EvidenceNo = "PRN001234567",
                                IssuedToEPRId = "3bd7b37e-edf2-4c0f-8051-b574cc5130ed",
                                EvidenceTonnes = 150,
                                EvidenceMaterial = "Plastic",
                                AccreditationYear = 2025,
                                EvidenceStatusCode = "EV-ACTIVE",
                                IssueDate = "2025-01-15T00:00:00Z",
                                IssuedByOrgName = "Issuer_org_name",
                                IssuedToOrgName = "Producer_org_name"
                            }
                        }
                    }
                ))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetProducersPatchRequests()
    {
        var requestsModel = new RequestModel { Methods = ["PATCH"], Path = "/odata/producers" };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public async Task<IList<LogEntryModel>> GetPrnPatchRequests()
    {
        var requestsModel = new RequestModel { Methods = ["PATCH"], Path = "/odata/PRNs" };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }
}