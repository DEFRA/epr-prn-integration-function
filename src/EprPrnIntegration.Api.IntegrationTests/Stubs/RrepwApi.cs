using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class RrepwApi(WireMockContext wiremock)
{
    public async Task<string> HasPrnUpdate(string prnNumber)
    {
        var id = Guid.NewGuid().ToString();
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/v1/packaging-recycling-notes/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new
                {
                    items = new[]
                    {
                        new
                        {
                            id,
                            prnNumber,
                            status = new
                            {
                                currentStatus = "AWAITING_ACCEPTANCE",
                                authorisedAt = "2025-01-15T10:30:00Z",
                                authorisedBy = new
                                {
                                    fullName = "John Doe",
                                    jobTitle = "Manager"
                                }
                            },
                            issuedByOrganisation = new
                            {
                                id = Guid.NewGuid().ToString(),
                                name = "Issuer Org"
                            },
                            issuedToOrganisation = new
                            {
                                id = Guid.NewGuid().ToString(),
                                name = "Recipient Org"
                            },
                            accreditation = new
                            {
                                id = Guid.NewGuid().ToString(),
                                accreditationNumber = "ACC-001",
                                accreditationYear = 2025,
                                material = "Plastic",
                                submittedToRegulator = "EA",
                                siteAddress = new
                                {
                                    line1 = "123 Test Street"
                                }
                            },
                            isDecemberWaste = false,
                            isExport = false,
                            tonnageValue = 100,
                            issuerNotes = "Test notes"
                        }
                    },
                    hasMore = false,
                    nextCursor = (string?)null
                }))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
        return id;
    }

    public async Task HasNoPrns()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/v1/packaging-recycling-notes/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new
                {
                    items = Array.Empty<object>(),
                    hasMore = false,
                    nextCursor = (string?)null
                }))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetPrnRequests()
    {
        var requestsModel = new RequestModel { Methods = ["GET"], Path = "/v1/packaging-recycling-notes/" };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }
}
