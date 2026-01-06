using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class RrepwApi(WireMockContext wiremock)
{
    public async Task HasPrnUpdates(
        string[] prnNumbers,
        string? cursor = null,
        string? nextCursor = null
    )
    {
        var items = prnNumbers
            .Select(prnNumber => new
            {
                id = Guid.NewGuid().ToString(),
                prnNumber,
                status = new
                {
                    currentStatus = "AWAITING_ACCEPTANCE",
                    authorisedAt = "2025-01-15T10:30:00Z",
                    authorisedBy = new { fullName = "John Doe", jobTitle = "Manager" },
                },
                issuedByOrganisation = new { id = Guid.NewGuid().ToString(), name = "Issuer Org" },
                issuedToOrganisation = new
                {
                    id = Guid.NewGuid().ToString(),
                    name = "Recipient Org",
                },
                accreditation = new
                {
                    id = Guid.NewGuid().ToString(),
                    accreditationNumber = "ACC-001",
                    accreditationYear = 2025,
                    material = "Plastic",
                    submittedToRegulator = "EA",
                    siteAddress = new { line1 = "123 Test Street" },
                },
                isDecemberWaste = false,
                isExport = false,
                tonnageValue = 100,
                issuerNotes = "Test notes",
            })
            .ToArray();

        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                {
                    var req = request.UsingGet().WithPath("/v1/packaging-recycling-notes");

                    if (cursor != null)
                    {
                        req.WithParams(() =>
                            new List<ParamModel>
                            {
                                new()
                                {
                                    Name = "cursor",
                                    Matchers = new[]
                                    {
                                        new MatcherModel
                                        {
                                            Name = "ExactMatcher",
                                            Pattern = cursor,
                                        },
                                    },
                                },
                            }
                        );
                    }
                })
                .WithResponse(response =>
                    response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBodyAsJson(
                            new
                            {
                                items,
                                hasMore = nextCursor != null,
                                nextCursor,
                            }
                        )
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsPrnAccept()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request
                        .UsingPost()
                        .WithPath(
                            new MatcherModel
                            {
                                Name = "RegexMatcher",
                                Pattern = @"/v1/packaging-recycling-notes/.+/accept",
                            }
                        )
                )
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsPrnReject()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request
                        .UsingPost()
                        .WithPath(
                            new MatcherModel
                            {
                                Name = "RegexMatcher",
                                Pattern = @"/v1/packaging-recycling-notes/.+/reject",
                            }
                        )
                )
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetPrnAcceptRequests()
    {
        var requestsModel = new RequestModel { Methods = ["POST"] };
        var allRequests = await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
        return allRequests.Where(r => r.Request.Path?.Contains("/accept") == true).ToList();
    }

    public async Task<IList<LogEntryModel>> GetPrnRejectRequests()
    {
        var requestsModel = new RequestModel { Methods = ["POST"] };
        var allRequests = await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
        return allRequests.Where(r => r.Request.Path?.Contains("/reject") == true).ToList();
    }

    public async Task<IList<LogEntryModel>> GetPrnPatchRequests()
    {
        var requestsModel = new RequestModel { Methods = ["POST"] };
        var allRequests = await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
        return allRequests
            .Where(r =>
                r.Request.Path?.Contains("/v1/packaging-recycling-notes/") == true
                && (
                    r.Request.Path?.Contains("/accept") == true
                    || r.Request.Path?.Contains("/reject") == true
                )
            )
            .ToList();
    }
}
