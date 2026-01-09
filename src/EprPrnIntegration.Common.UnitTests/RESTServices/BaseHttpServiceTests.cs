using EprPrnIntegration.Common.RESTServices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.RESTServices;

public class BaseHttpServiceTests
{
    private readonly TestableBaseHttpService _sut;

    public BaseHttpServiceTests()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        _sut = new TestableBaseHttpService(
            httpContextAccessor.Object,
            httpClientFactory.Object,
            "http://localhost:9090",
            NullLogger.Instance
        );
    }

    [Fact]
    public void BuildUrl_WithEmptyPath_ReturnsBaseUrlWithTrailingSlash()
    {
        var result = _sut.TestBuildUrl("");

        result.Should().Be("http://localhost:9090/");
    }

    [Fact]
    public void BuildUrl_WithNullPath_ReturnsBaseUrlWithTrailingSlash()
    {
        var result = _sut.TestBuildUrl(null!);

        result.Should().Be("http://localhost:9090/");
    }

    [Fact]
    public void BuildUrl_WithWhitespacePath_ReturnsBaseUrlWithTrailingSlash()
    {
        var result = _sut.TestBuildUrl("   ");

        result.Should().Be("http://localhost:9090/");
    }

    [Fact]
    public void BuildUrl_WithSimplePath_ReturnsUrlWithTrailingSlash()
    {
        var result = _sut.TestBuildUrl("api/v1/prn");

        result.Should().Be("http://localhost:9090/api/v1/prn/");
    }

    [Fact]
    public void BuildUrl_WithPathStartingWithSlash_ReturnsUrlWithTrailingSlash()
    {
        var result = _sut.TestBuildUrl("/api/v1/prn");

        result.Should().Be("http://localhost:9090/api/v1/prn/");
    }

    [Fact]
    public void BuildUrl_WithPathEndingWithSlash_ReturnsUrlWithTrailingSlash()
    {
        var result = _sut.TestBuildUrl("api/v1/prn/");

        result.Should().Be("http://localhost:9090/api/v1/prn/");
    }

    [Fact]
    public void BuildUrl_WithQueryString_ReturnsUrlWithoutTrailingSlash()
    {
        var result = _sut.TestBuildUrl("api/v1/prn?id=123");

        result.Should().Be("http://localhost:9090/api/v1/prn?id=123");
    }

    [Fact]
    public void BuildUrl_WithQueryStringAndLeadingSlash_ReturnsUrlWithoutTrailingSlash()
    {
        var result = _sut.TestBuildUrl("/api/v1/prn?id=123");

        result.Should().Be("http://localhost:9090/api/v1/prn?id=123");
    }

    [Fact]
    public void BuildUrl_WithMultipleQueryParameters_ReturnsUrlWithoutTrailingSlash()
    {
        var result = _sut.TestBuildUrl(
            "api/v2/prn/modified-prns?dateFrom=2024-01-01&dateTo=2024-12-31"
        );

        result
            .Should()
            .Be(
                "http://localhost:9090/api/v2/prn/modified-prns?dateFrom=2024-01-01&dateTo=2024-12-31"
            );
    }

    [Fact]
    public void BuildUrl_WithBaseUrlHavingTrailingSlash_ReturnsCorrectUrl()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var sut = new TestableBaseHttpService(
            httpContextAccessor.Object,
            httpClientFactory.Object,
            "http://localhost:9090/",
            NullLogger.Instance
        );

        var result = sut.TestBuildUrl("api/v1/prn");

        result.Should().Be("http://localhost:9090/api/v1/prn/");
    }

    [Fact]
    public void BuildUrl_WithBaseUrlHavingMultipleTrailingSlashes_ReturnsCorrectUrl()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var sut = new TestableBaseHttpService(
            httpContextAccessor.Object,
            httpClientFactory.Object,
            "http://localhost:9090///",
            NullLogger.Instance
        );

        var result = sut.TestBuildUrl("api/v1/prn");

        result.Should().Be("http://localhost:9090/api/v1/prn/");
    }

    [Fact]
    public void BuildUrl_WithPathHavingMultipleLeadingSlashes_ReturnsCorrectUrl()
    {
        var result = _sut.TestBuildUrl("///api/v1/prn");

        result.Should().Be("http://localhost:9090/api/v1/prn/");
    }

    [Fact]
    public void BuildUrl_WithComplexPath_ReturnsCorrectUrl()
    {
        var result = _sut.TestBuildUrl("v1/packaging-recycling-notes/PRN12345/accept");

        result.Should().Be("http://localhost:9090/v1/packaging-recycling-notes/PRN12345/accept/");
    }

    [Fact]
    public void BuildUrl_WithOrganisationsPath_ReturnsCorrectUrl()
    {
        var result = _sut.TestBuildUrl("organisations/abc-123");

        result.Should().Be("http://localhost:9090/organisations/abc-123/");
    }

    [Fact]
    public void BuildUrl_WithEncodedQueryString_DecodesAndReturnsWithoutTrailingSlash()
    {
        // Uri class decodes %20 to space
        var result = _sut.TestBuildUrl("api/search?name=John%20Doe");

        result.Should().Be("http://localhost:9090/api/search?name=John Doe");
    }

    [Fact]
    public void BuildUrl_WithFragmentInPath_AddsTrailingSlash()
    {
        // Fragments don't count as query strings, so trailing slash is added
        var result = _sut.TestBuildUrl("api/v1/docs#section");

        result.Should().Be("http://localhost:9090/api/v1/docs#section/");
    }

    private class TestableBaseHttpService : BaseHttpService
    {
        public TestableBaseHttpService(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            string baseUrl,
            ILogger logger
        )
            : base(httpContextAccessor, httpClientFactory, baseUrl, logger) { }

        public string TestBuildUrl(string path) => BuildUrl(path);
    }
}
