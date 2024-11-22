using EprPrnIntegration.Common.Service;
using EprPrnIntegration.Common.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;
using Constants = EprPrnIntegration.Common.Constants;

namespace EprPrnIntegration.Api.Tests;

public class PrnFunctionTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfigurationService> _configurationServiceMock = new();
    private readonly Mock<ILogger<PrnFunction>> _loggerMock = new();
    private readonly HttpClient _mockHttpClient;

    public PrnFunctionTests()
    {
        // Set up a mock HttpClient
        _mockHttpClient = new HttpClient(new MockHttpMessageHandler());
        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient(Constants.HttpClientNames.Npwd))
            .Returns(_mockHttpClient);
    }

    [Fact]
    public async Task Run_ValidResponse_ReturnsOkResult()
    {
        // Arrange
        var baseAddress = "https://testapi.com";
        _configurationServiceMock.Setup(config => config.GetNpwdApiBaseUrl()).Returns(baseAddress);

        var handler = new MockHttpMessageHandler("{\"data\": \"Test PRNs\"}", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseAddress)
        };

        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient(Constants.HttpClientNames.Npwd))
            .Returns(httpClient);

        var function = new PrnFunction(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _loggerMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Run_HttpClientThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var baseAddress = "https://testapi.com";
        _configurationServiceMock.Setup(config => config.GetNpwdApiBaseUrl()).Returns(baseAddress);

        var handler = new MockHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseAddress)
        };

        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient(Constants.HttpClientNames.Npwd))
            .Returns(httpClient);

        var function = new PrnFunction(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _loggerMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Run_ConfigurationServiceReturnsNull_ThrowsException()
    {
        // Arrange
        _configurationServiceMock.Setup(config => config.GetNpwdApiBaseUrl()).Returns((string)null);

        var function = new PrnFunction(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _loggerMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        var statusCodeResult = (StatusCodeResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }
}