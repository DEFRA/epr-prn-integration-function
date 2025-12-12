using System.Net;
using System.Text;
using System.Text.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Middleware;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace EprPrnIntegration.Common.UnitTests.Middleware;

public class WasteOrganisationsApiAuthorisationHandlerTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptions<WasteOrganisationsApiConfiguration>> _configMock;
    private readonly Mock<ILogger<WasteOrganisationsApiAuthorisationHandler>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly WasteOrganisationsApiConfiguration _config;

    public WasteOrganisationsApiAuthorisationHandlerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IOptions<WasteOrganisationsApiConfiguration>>();
        _loggerMock = new Mock<ILogger<WasteOrganisationsApiAuthorisationHandler>>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();

        _config = new WasteOrganisationsApiConfiguration
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            AccessTokenUrl = "https://cognito.example.com/oauth/token"
        };

        _configMock.Setup(c => c.Value).Returns(_config);

        // Clear the static cached token before each test
        WasteOrganisationsApiAuthorisationHandler.ClearCachedToken();
    }

    public void Dispose()
    {
        // Clear the static cached token after each test
        WasteOrganisationsApiAuthorisationHandler.ClearCachedToken();
    }

    [Fact]
    public async Task WhenCredentialsExist_ShouldFetchTokenAndAddAuthorizationHeader()
    {
        // Arrange
        var dummyToken = "test-access-token-12345";
        SetupCognitoResponse(dummyToken);
        SetupHttpStubResponse();

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization!.Parameter.Should().Be(dummyToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenRequest_ShouldUseBasicAuthWithEncodedCredentials()
    {
        // Arrange
        HttpRequestMessage? capturedTokenRequest = null;
        SetupCognitoResponse(callback: (req, _) => capturedTokenRequest = req);
        SetupHttpStubResponse();

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);

        // Act
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test"));

        // Assert
        capturedTokenRequest.Should().NotBeNull();
        capturedTokenRequest!.Headers.Authorization.Should().NotBeNull();
        capturedTokenRequest.Headers.Authorization!.Scheme.Should().Be("Basic");

        var expectedCredentials = $"{_config.ClientId}:{_config.ClientSecret}";
        var expectedEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedCredentials));
        capturedTokenRequest.Headers.Authorization.Parameter.Should().Be(expectedEncoded);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenRequest_ShouldSendFormDataWithCorrectFields()
    {
        // Arrange
        HttpRequestMessage? capturedTokenRequest = null;
        SetupCognitoResponse(callback: (req, _) => capturedTokenRequest = req);
        SetupHttpStubResponse();

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);

        // Act
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test"));

        // Assert
        capturedTokenRequest.Should().NotBeNull();
        capturedTokenRequest!.Content.Should().BeOfType<FormUrlEncodedContent>();

        var formContent = await capturedTokenRequest.Content!.ReadAsStringAsync();
        formContent.Should().Contain("grant_type=client_credentials");
        formContent.Should().Contain($"client_id={_config.ClientId}");
        formContent.Should().Contain($"client_secret={_config.ClientSecret}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenTokenIsCached_SecondRequestShouldUseCacheWithoutFetchingAgain()
    {
        // Arrange
        var tokenFetchCount = 0;
        SetupCognitoResponse(callback: (_, _) => tokenFetchCount++);
        SetupHttpStubResponse();

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);

        // Act
        var response1 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test1"));
        var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test2"));
        var response3 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test3"));

        // Assert
        tokenFetchCount.Should().Be(1, "token should only be fetched once and then cached");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenTokenResponseHasNullAccessToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupCognitoResponse(accessToken: null);

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test")));

        exception.Message.Should().Be("Failed to retrieve access token from Cognito");
    }

    [Fact]
    public async Task WhenTokenEndpointReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        SetupCognitoResponse(
            errorContent: "Invalid credentials",
            statusCode: HttpStatusCode.Unauthorized);

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test")));
    }
    
    [Theory]
    [InlineData(null, "test-client-secret")]
    [InlineData("", "test-client-secret")]
    [InlineData("test-client-id", null)]
    [InlineData("test-client-id", "")]
    public async Task WhenCredentialsAreMissing_ShouldSkipAuthAndCallBaseHandler(string? clientId, string? clientSecret)
    {
        // Arrange
        _config.ClientId = clientId!;
        _config.ClientSecret = clientSecret!;
        SetupHttpStubResponse();

        var handler = new WasteOrganisationsApiAuthorisationHandler(
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        request.Headers.Authorization.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void SetupHttpStubResponse()
    {
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("success")
            });
    }

    private void SetupCognitoResponse(
        string? accessToken = "test-token",
        string? errorContent = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Action<HttpRequestMessage, CancellationToken>? callback = null)
    {
        string content;
        if (errorContent != null)
        {
            content = errorContent;
        }
        else
        {
            var tokenResponse = new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600
            };
            content = JsonSerializer.Serialize(tokenResponse);
        }

        var cognitoHandlerMock = new Mock<HttpMessageHandler>();
        var setup = cognitoHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        if (callback != null)
        {
            setup.Callback(callback)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }
        else
        {
            setup.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        }

        var cognitoClient = new HttpClient(cognitoHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(cognitoClient);
    }
}
