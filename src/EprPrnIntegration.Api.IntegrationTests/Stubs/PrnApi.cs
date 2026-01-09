using System.Net;
using EprPrnIntegration.Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class PrnApi(WireMockContext wiremock)
{
    public async Task AcceptsPrnDetails()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingPost().WithPath("/api/v1/prn/prn-details/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsPrnV2()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingPost().WithPath("/api/v2/prn/"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsPrnV2ForId(string id)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request
                        .UsingPost()
                        .WithPath("/api/v2/prn/")
                        .WithBody(body =>
                            body.WithMatcher(matcher =>
                                matcher
                                    .WithName("JsonPathMatcher")
                                    .WithPattern("$.PrnNumber")
                                    .WithPatterns(id)
                            )
                        )
                )
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsPrnV2WithTransientFailures(
        HttpStatusCode failureResponse,
        int failureCount
    )
    {
        await wiremock.WithEndpointRecoveringFromTransientFailures(
            request => request.UsingPost().WithPath("/api/v2/prn/"),
            response => response.WithStatusCode(HttpStatusCode.Accepted),
            response => response.WithStatusCode(failureResponse),
            failureCount
        );
    }

    public async Task AcceptsPrnV2WithNonTransientFailure(
        string id,
        HttpStatusCode failureResponse = HttpStatusCode.BadRequest
    )
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request
                        .UsingPost()
                        .WithPath("/api/v2/prn/")
                        .WithBody(body =>
                            body.WithMatcher(matcher =>
                                matcher
                                    .WithName("JsonPathMatcher")
                                    .WithPattern("$.PrnNumber")
                                    .WithPatterns(id)
                            )
                        )
                )
                .WithResponse(response => response.WithStatusCode(failureResponse))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task AcceptsSyncStatus()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingPost().WithPath("/api/v1/prn/updatesyncstatus/")
                )
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task HasUpdateFor(string evidenceNo)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingGet().WithPath("/api/v1/prn/ModifiedPrnsByDate")
                )
                .WithResponse(response =>
                    response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBodyAsJson(
                            new[]
                            {
                                new
                                {
                                    evidenceNo,
                                    evidenceStatusCode = "EV-ACCEP",
                                    statusDate = "2025-01-15T10:30:00Z",
                                },
                            }
                        )
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetUpdateSyncStatusRequests()
    {
        var requestsModel = new RequestModel
        {
            Methods = ["POST"],
            Path = "/api/v1/prn/updatesyncstatus/",
        };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public async Task<IList<LogEntryModel>> GetDetailRequests()
    {
        var requestsModel = new RequestModel
        {
            Methods = ["POST"],
            Path = "/api/v1/prn/prn-details/",
        };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public async Task<IList<LogEntryModel>> GetPrnDetailsUpdateV2()
    {
        var requestsModel = new RequestModel { Methods = ["POST"], Path = "/api/v2/prn/" };
        return await wiremock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }

    public async Task HasUpdatedPrns(List<PrnUpdateStatus> payload)
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingGet().WithPath("/api/v2/prn/modified-prns"))
                .WithResponse(response =>
                    response.WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(payload)
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }
}
