using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class WireMockStubs
{
    public static async Task NpwdAcceptsProducerPatch(this WireMockContext wiremock)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPatch().WithPath("/odata/producers"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public static async Task NpwdAcceptsPrnPatch(this WireMockContext wiremock)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPatch().WithPath("/odata/PRNs"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public static async Task CommonDataApiHasUpdateFor(this WireMockContext wiremock, string name)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/api/producer-details/get-updated-producers"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new[]
                {
                    new
                    {
                        organisationName = name,
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
    }

    public static async Task NpwdHasIssuedPrns(this WireMockContext wiremock, string accreditationNo)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
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

    public static async Task PrnApiAcceptsPrnDetails(this WireMockContext wiremock)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPost().WithPath("/api/v1/prn/prn-details/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public static async Task PrnApiAcceptsSyncStatus(this WireMockContext wiremock)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingPost().WithPath("/api/v1/prn/updatesyncstatus/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public static async Task PrnApiHasUpdateFor(this WireMockContext wiremock, string evidenceNo)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
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

    public static async Task AccountServiceValidatesIssuedEpr(this WireMockContext wiremock)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/api/organisations/validate-issued-epr-id"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public static async Task AccountServiceHasPersonEmailForEpr(this WireMockContext wiremock)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/api/organisations/person-emails"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new[]
                {
                    new
                    {
                        FirstName = "firstName",
                        LastName = "lastName",
                        Email = "fake-email-person@fake-email.domain.co.uk"
                    }
                }))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public static async Task<IList<LogEntryModel>> GetNpwdProducersPatchRequests(this WireMockContext wiremock)
    {
        var requestsModel = new RequestModel { Methods = ["PATCH"], Path = "/odata/producers" };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public static async Task<IList<LogEntryModel>> GetNpwdPrnPatchRequests(this WireMockContext wiremock)
    {
        var requestsModel = new RequestModel { Methods = ["PATCH"], Path = "/odata/PRNs" };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public static async Task<IList<LogEntryModel>> GetPrnUpdateSyncStatusRequests(this WireMockContext wiremock)
    {
        var requestsModel = new RequestModel { Methods = ["POST"], Path = "/api/v1/prn/updatesyncstatus/" };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public static async Task<IList<LogEntryModel>> GetPrnDetailRequests(this WireMockContext wiremock)
    {
        var requestsModel = new RequestModel { Methods = ["POST"], Path = "/api/v1/prn/prn-details/" };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }
}