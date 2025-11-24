using System.Net;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class AccountApi(WireMockContext wiremock)
{
    public async Task ValidatesIssuedEpr()
    {
        var mappingBuilder = wiremock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/api/organisations/validate-issued-epr-id"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task HasPersonEmailForEpr()
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
}