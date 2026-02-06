using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.RrepwService;

public class RrepwServiceTests
{
    private readonly Common.RESTServices.RrepwService.RrepwService _service;
    private readonly TestHttpMessageHandler _mockHandler;
    private readonly string _testUrl = "http://test";

    private class TestHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;
        public HttpRequestMessage? Request { get; private set; } = null;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Request = request;
            return Task.FromResult(_response);
        }
    }

    public RrepwServiceTests()
    {
        var mockContextAccesser = new Mock<IHttpContextAccessor>();
        var mockClientFactory = new Mock<IHttpClientFactory>();
        _mockHandler = new TestHttpMessageHandler(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        );
        var mockClient = new HttpClient(_mockHandler);
        mockClientFactory.Setup(f => f.CreateClient(HttpClientNames.Rrepw)).Returns(mockClient);
        var configMock = new Mock<IOptions<RrepwApiConfiguration>>();
        configMock.Setup(c => c.Value).Returns(new RrepwApiConfiguration { BaseUrl = _testUrl });
        _service = new Common.RESTServices.RrepwService.RrepwService(
            mockContextAccesser.Object,
            mockClientFactory.Object,
            new Mock<ILogger<Common.RESTServices.RrepwService.RrepwService>>().Object,
            configMock.Object
        );
    }

    [Fact]
    public async Task ShouldUpdatePrns_Accepted()
    {
        var prn = new PrnUpdateStatus()
        {
            AccreditationYear = "2024",
            PrnNumber = "123",
            PrnStatusId = (int)EprnStatus.ACCEPTED,
            SourceSystemId = "something",
            StatusDate = DateTime.Now,
        };
        await _service.UpdatePrn(prn);
        _mockHandler.Request!.Method.Should().Be(HttpMethod.Post);
        _mockHandler
            .Request.RequestUri.Should()
            .Be($"{_testUrl}/v1/packaging-recycling-notes/{prn.SourceSystemId}/accept/");
        var content = await _mockHandler.Request.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var value = doc.RootElement.GetProperty("acceptedAt").GetDateTime();
        value.Should().Be(prn.StatusDate);
    }

    [Fact]
    public async Task ShouldUpdatePrns_Rejected()
    {
        var prn = new PrnUpdateStatus
        {
            AccreditationYear = "2024",
            PrnNumber = "123",
            PrnStatusId = (int)EprnStatus.REJECTED,
            SourceSystemId = "something",
            StatusDate = DateTime.Now,
        };
        await _service.UpdatePrn(prn);
        _mockHandler.Request!.Method.Should().Be(HttpMethod.Post);
        _mockHandler
            .Request.RequestUri.Should()
            .Be($"{_testUrl}/v1/packaging-recycling-notes/{prn.SourceSystemId}/reject/");
        var content = await _mockHandler.Request.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var value = doc.RootElement.GetProperty("rejectedAt").GetDateTime();
        value.Should().Be(prn.StatusDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldUpdatePrns_NullOrEmptySourceSystemId(string? ssi)
    {
        var prn = new PrnUpdateStatus
        {
            AccreditationYear = "2024",
            PrnNumber = "123",
            PrnStatusId = (int)EprnStatus.ACCEPTED,
            SourceSystemId = ssi!,
            StatusDate = DateTime.Now,
        };
        await _service.UpdatePrn(prn);
        _mockHandler.Request.Should().BeNull();
    }

    [Theory]
    [InlineData((int)EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData((int)EprnStatus.CANCELLED)]
    [InlineData(100)]
    public async Task ShouldUpdatePrns_InvalidStatus(int status)
    {
        var prn = new PrnUpdateStatus
        {
            AccreditationYear = "2024",
            PrnNumber = "123",
            PrnStatusId = status,
            SourceSystemId = "something",
            StatusDate = DateTime.Now,
        };
        await _service.UpdatePrn(prn);
        _mockHandler.Request.Should().BeNull();
    }
}
