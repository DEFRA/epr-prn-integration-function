using System.Net;
using FluentAssertions;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

[Collection("WireMock")]
public class WireMockIntegrationTest(WireMockFixture wireMockFixture) : IntegrationTestBase
{
    [Fact]
    public async Task NoMappingsShouldBeFound()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(WireMockFixture.BaseUri)
        };

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be("{\"Status\":\"No matching mapping found\"}");
    }
    
    [Fact]
    public async Task WhenMappingIsSet_ShouldBeFound()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(WireMockFixture.BaseUri)
        };
        
        var mappingBuilder = wireMockFixture.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/some-path"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);

        var response = await client.GetAsync("/some-path");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}