using System.Net;
using FluentAssertions;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

public class WireMockIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private WireMockContext _wireMockContext = null!;
    
    [Fact]
    public async Task NoMappingsShouldBeFound()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(WireMockContext.BaseUri)
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
            BaseAddress = new Uri(WireMockContext.BaseUri)
        };
        
        var mappingBuilder = _wireMockContext.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request.UsingGet().WithPath("/some-path"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.OK))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);

        var response = await client.GetAsync("/some-path");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public async Task InitializeAsync()
    {
        _wireMockContext = new WireMockContext();
        await _wireMockContext.InitializeAsync();
    }

    public async Task DisposeAsync() => await _wireMockContext.DisposeAsync();
}