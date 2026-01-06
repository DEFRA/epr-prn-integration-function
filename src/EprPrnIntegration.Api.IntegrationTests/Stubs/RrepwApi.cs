using System.Net;
using WireMock.Admin.Mappings;
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
}
