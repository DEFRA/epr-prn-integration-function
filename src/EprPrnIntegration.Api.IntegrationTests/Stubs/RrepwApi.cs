using System.Net;
using EprPrnIntegration.Common.Models.Rrepw;
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
        var items = CreatePrns(prnNumbers);
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => CreatePrnRequest(request, cursor))
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

    public async Task HasPrnUpdatesWithTransientFailures(
        string[] prnNumbers,
        HttpStatusCode failureStatusCode,
        int failuresCount
    )
    {
        var items = CreatePrns(prnNumbers);
        await wiremock.WithEndpointRecoveringFromTransientFailures(
            request => CreatePrnRequest(request),
            response =>
                response
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new { items, hasMore = false }),
            r => r.WithStatusCode(failureStatusCode),
            failuresCount
        );
    }

    public static RequestModelBuilder CreatePrnRequest(
        RequestModelBuilder request,
        string? cursor = null
    )
    {
        var req = request.UsingGet().WithPath("/v1/packaging-recycling-notes");

        if (cursor != null)
        {
            req.WithParams(() =>
                [
                    new()
                    {
                        Name = "cursor",
                        Matchers = [new MatcherModel { Name = "ExactMatcher", Pattern = cursor }],
                    },
                ]
            );
        }

        return req;
    }

    private static List<PackagingRecyclingNote> CreatePrns(string[] prnNumbers)
    {
        var items = prnNumbers
            .Select(prnNumber => new PackagingRecyclingNote
            {
                Id = Guid.NewGuid().ToString(),
                PrnNumber = prnNumber,
                Status = new Status
                {
                    CurrentStatus = "AWAITING_ACCEPTANCE",
                    AuthorisedAt = DateTime.Parse("2025-01-15T10:30:00Z"),
                    AuthorisedBy = new UserSummary { FullName = "John Doe", JobTitle = "Manager" },
                },
                IssuedByOrganisation = new Organisation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Issuer Org",
                },
                IssuedToOrganisation = new Organisation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Recipient Org",
                },
                Accreditation = new Accreditation
                {
                    Id = Guid.NewGuid().ToString(),
                    AccreditationNumber = "ACC-001",
                    AccreditationYear = 2025,
                    Material = "Plastic",
                    SubmittedToRegulator = "EA",
                    SiteAddress = new Address { Line1 = "123 Test Street" },
                },
                IsDecemberWaste = false,
                IsExport = false,
                TonnageValue = 100,
                IssuerNotes = "Test notes",
            })
            .ToList();
        return items;
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

    public async Task AcceptsPrnAcceptWithFailures(HttpStatusCode failureResponse, int failureCount)
    {
        await wiremock.WithEndpointRecoveringFromTransientFailures(
            request =>
                request
                    .UsingPost()
                    .WithPath(
                        new MatcherModel
                        {
                            Name = "RegexMatcher",
                            Pattern = @"/v1/packaging-recycling-notes/.+/accept",
                        }
                    ),
            response => response.WithStatusCode(HttpStatusCode.OK),
            response => response.WithStatusCode(failureResponse),
            failureCount
        );
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

    public async Task AcceptsPrnRejectWithTransientFailures(int failureResponse, int failureCount)
    {
        await wiremock.WithEndpointRecoveringFromTransientFailures(
            request =>
                request
                    .UsingPost()
                    .WithPath(
                        new MatcherModel
                        {
                            Name = "RegexMatcher",
                            Pattern = @"/v1/packaging-recycling-notes/.+/reject",
                        }
                    ),
            response => response.WithStatusCode(HttpStatusCode.OK),
            response => response.WithStatusCode(failureResponse),
            failureCount
        );
    }

    public async Task<IList<LogEntryModel>> GetPrnRequests()
    {
        var requestsModel = new RequestModel { Methods = ["GET"] };
        var allRequests = await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
        return
        [
            .. allRequests.Where(r =>
                r.Request.Path?.Contains("/v1/packaging-recycling-notes") == true
            ),
        ];
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
