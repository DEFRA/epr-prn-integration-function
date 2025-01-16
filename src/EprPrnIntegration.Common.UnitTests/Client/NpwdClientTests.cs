using AutoFixture;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace EprPrnIntegration.Common.UnitTests.Client
{
    public class NpwdClientTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IOptions<NpwdIntegrationConfiguration>> _npwdIntegrationConfigMock;
        private readonly HttpClient _httpClient;
        private Mock<ILogger<NpwdClient>> _mockLogger;
        private static readonly IFixture _fixture = new Fixture();
        private NpwdClient _npwdClient;
        private NpwdIntegrationConfiguration _npwdConfig;

        public NpwdClientTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<NpwdClient>>();
            _npwdIntegrationConfigMock = new Mock<IOptions<NpwdIntegrationConfiguration>>();

            _npwdConfig = _fixture.Create<NpwdIntegrationConfiguration>();
            _npwdConfig.BaseUrl = "http://localhost";
            _npwdIntegrationConfigMock.Setup(m => m.Value).Returns(_npwdConfig);
            // Mock HttpMessageHandler
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Success")
                });

            _httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(HttpClientNames.Npwd))
                .Returns(_httpClient);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _npwdIntegrationConfigMock.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task UpdateProducerList_ShouldReturnSuccessResponse_WhenProducersAreUpdatedSuccessfully()
        {
            // Arrange
            var updatedProducers = new List<Producer>
            {
                new Producer
                {
                    ProducerName = "Test Producer",
                    CompanyRegNo = "12345678",
                    Postcode = "12345"
                }
            };

            // Act
            var response = await _npwdClient.Patch(updatedProducers, NpwdApiPath.Producers);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Success", content);
        }

        [Fact]
        public async Task UpdateProducerList_ShouldHandleErrorResponse_WhenApiReturnsError()
        {
            // Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Error")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(HttpClientNames.Npwd))
                .Returns(httpClient);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _npwdIntegrationConfigMock.Object, _mockLogger.Object);
            // Act
            var response = await _npwdClient.Patch(new List<Producer>(), NpwdApiPath.Producers);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Error", content);
        }

        [Fact]
        public async Task UpdateProducerList_ShouldThrowException_WhenBaseAddressIsNull()
        {
            // Arrange
            _npwdConfig = _fixture.Create<NpwdIntegrationConfiguration>();
            _npwdConfig.BaseUrl = null;

            _npwdIntegrationConfigMock.Setup(m => m.Value).Returns(_npwdConfig);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _npwdIntegrationConfigMock.Object, _mockLogger.Object);
            // Act & Assert
            await Assert.ThrowsAsync<UriFormatException>(() => _npwdClient.Patch(new List<Producer>(), NpwdApiPath.Producers));
        }

        [Fact]
        public async Task GetIssuedPrns_ShouldThrowException_WhenBaseAddressIsNull()
        {
            // Arrange
            _npwdConfig = _fixture.Create<NpwdIntegrationConfiguration>();
            _npwdConfig.BaseUrl = string.Empty;

            _npwdIntegrationConfigMock.Setup(m => m.Value).Returns(_npwdConfig);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _npwdIntegrationConfigMock.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<UriFormatException>(() => _npwdClient.GetIssuedPrns("1 eq 1"));
        }

        [Fact]
        public async Task GetIssuedPrns_ShouldCallNpwdGetPrnsWithPassedFilter_and_ReturnAllIssuedPrns()
        {
            var npwdGetIssuedPrnsResponse = _fixture.Create<GetPrnsResponseModel>();

            var filter = "1 eq 1";
            var baseUrl = "http://localhost/";

            //Respnse witth next link
            npwdGetIssuedPrnsResponse.NextLink = $"{baseUrl}{NpwdApiPath.Prns}?$filter={filter}";
            var jsonResponse1 = JsonConvert.SerializeObject(npwdGetIssuedPrnsResponse);

            //Repsone with next null
            npwdGetIssuedPrnsResponse.NextLink = null;
            var jsonReponse2 = JsonConvert.SerializeObject(npwdGetIssuedPrnsResponse);

            var expectedRequestUri = new Uri($"{baseUrl}{NpwdApiPath.Prns}?$filter={filter}");

            // Arrange
            _npwdConfig = _fixture.Create<NpwdIntegrationConfiguration>();
            _npwdConfig.BaseUrl = baseUrl;

            _npwdIntegrationConfigMock.Setup(m => m.Value).Returns(_npwdConfig);

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse1)
                })
                .ReturnsAsync(new HttpResponseMessage
                                {
                                    StatusCode = HttpStatusCode.OK,
                                    Content = new StringContent(jsonReponse2)
                                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(baseUrl)
            };

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(HttpClientNames.Npwd))
                .Returns(httpClient);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _npwdIntegrationConfigMock.Object, _mockLogger.Object);

            var result = await _npwdClient.GetIssuedPrns(filter);
            // Act & Assert
            httpMessageHandlerMock.Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(
                    req => req.Method == HttpMethod.Get
                           && req.RequestUri == expectedRequestUri),
                ItExpr.IsAny<CancellationToken>());

            result.Should().BeEquivalentTo([..npwdGetIssuedPrnsResponse.Value, ..npwdGetIssuedPrnsResponse.Value]);
        }

        [Fact]
        public async Task GetIssuedPrns_ShouldThowException_if_npwdRequest_fails()
        {
            var filter = "1 eq 1";
            var baseUrl = "http://localhost";

            // Arrange
            _npwdConfig = _fixture.Create<NpwdIntegrationConfiguration>();
            _npwdConfig.BaseUrl = baseUrl;

            _npwdIntegrationConfigMock.Setup(m => m.Value).Returns(_npwdConfig);

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = new StringContent("ServerUnavialbe")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(baseUrl)
            };

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(HttpClientNames.Npwd))
                .Returns(httpClient);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _npwdIntegrationConfigMock.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _npwdClient.GetIssuedPrns(filter));
        }

    }
}