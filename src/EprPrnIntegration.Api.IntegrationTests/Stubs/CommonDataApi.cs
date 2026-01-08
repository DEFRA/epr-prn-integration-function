using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class CommonDataApi(WireMockContext wireMock)
{
    public async Task HasUpdateFor(string name)
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingGet().WithPath("/api/producer-details/get-updated-producers")
                )
                .WithResponse(response =>
                    response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBodyAsJson(
                            new[]
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
                                    updatedDateTime = "2025-01-15T10:30:00Z",
                                },
                            }
                        )
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<string> HasV2UpdateFor(string name)
    {
        var id = Guid.NewGuid().ToString();
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingGet().WithPath("/api/producer-details/updated-producers")
                )
                .WithResponse(response =>
                    response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBodyAsJson(
                            new[]
                            {
                                new
                                {
                                    peprid = id,
                                    organisationName = name,
                                    tradingName = "Acme Plastics",
                                    organisationType = "CS",
                                    status = "registered",
                                    companiesHouseNumber = "12345678",
                                    addressLine1 = "123 Industrial Estate",
                                    addressLine2 = "Unit 5",
                                    town = "Manchester",
                                    county = "Greater Manchester",
                                    country = "England",
                                    postcode = "M1 1AA",
                                    businessCountry = "England",
                                    updatedDateTime = "2025-01-15T10:30:00Z",
                                    registrationYear = "2025",
                                },
                            }
                        )
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);

        return id;
    }

    public async Task<IList<string>> HasV2MultipleUpdates(int amount)
    {
        var ids = Enumerable.Range(0, amount).Select(_ => Guid.NewGuid().ToString()).ToList();

        var producers = ids.Select(id => new
        {
            peprid = id,
            organisationName = $"dummy name {id}",
            tradingName = "Acme Plastics",
            organisationType = "CS",
            status = "registered",
            companiesHouseNumber = "12345678",
            addressLine1 = "123 Industrial Estate",
            addressLine2 = "Unit 5",
            town = "Manchester",
            county = "Greater Manchester",
            country = "England",
            postcode = "M1 1AA",
            businessCountry = "England",
            updatedDateTime = "2025-01-15T10:30:00Z",
            registrationYear = "2025",
        });

        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingGet().WithPath("/api/producer-details/updated-producers")
                )
                .WithResponse(response =>
                    response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(producers)
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);

        return ids;
    }

    public async Task HasNoV2Updates()
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingGet().WithPath("/api/producer-details/updated-producers")
                )
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.NoContent))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetUpdatedProducersRequests()
    {
        var requestsModel = new RequestModel
        {
            Methods = ["GET"],
            Path = "/api/producer-details/updated-producers",
        };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public async Task<string> HasV2UpdateWithTransientFailures(string name)
    {
        var id = Guid.NewGuid().ToString();

        var responseData = new[]
        {
            new
            {
                peprid = id,
                organisationName = name,
                tradingName = "Acme Plastics",
                organisationType = "CS",
                status = "registered",
                companiesHouseNumber = "12345678",
                addressLine1 = "123 Industrial Estate",
                addressLine2 = "Unit 5",
                town = "Manchester",
                county = "Greater Manchester",
                country = "England",
                postcode = "M1 1AA",
                businessCountry = "England",
                updatedDateTime = "2025-01-15T10:30:00Z",
                registrationYear = "2025",
            },
        };

        await wireMock.WithEndpointRecoveringFromTransientFailures(
            request => request.UsingGet().WithPath("/api/producer-details/updated-producers"),
            response => response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(responseData),
            response => response.WithStatusCode(HttpStatusCode.ServiceUnavailable),
            3
        );

        return id;
    }
}
