using AutoFixture;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace EprPrnIntegration.Common.UnitTests.Client
{
    public class NpwdClientTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfigurationService> _configurationServiceMock;
        private readonly HttpClient _httpClient;
        private Mock<ILogger<NpwdClient>> _mockLogger;
        private static readonly IFixture _fixture = new Fixture();
        private NpwdClient _npwdClient;

        public NpwdClientTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationServiceMock = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<NpwdClient>>();
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

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _mockLogger.Object);
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
                    Postcode = "12345",
                    StatusCode = "Active"
                }
            };

            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns("http://localhost");

            // Act
            var response = await _npwdClient.Patch(updatedProducers, NpwdApiPath.UpdateProducers);

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
                .Setup<Task<HttpResponseMessage>>(
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

            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns("http://localhost");

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _mockLogger.Object);
            // Act
            var response = await _npwdClient.Patch(new List<Producer>(), NpwdApiPath.UpdateProducers);

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
            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns((string)null); // Simulate null base address

            // Act & Assert
            await Assert.ThrowsAsync<UriFormatException>(() => _npwdClient.Patch(new List<Producer>(), NpwdApiPath.UpdateProducers));
        }

        [Fact]
        public async Task GetIssuedPrns_ShouldThrowException_WhenBaseAddressIsNull()
        {
            // Arrange
            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns(string.Empty); // Simulate null base address

            // Act & Assert
            await Assert.ThrowsAsync<UriFormatException>(() => _npwdClient.GetIssuedPrns("1 eq 1"));
        }

        [Fact]
        public async Task GetIssuedPrns_ShouldCallNpwdGetPrnsWithPassedFilter_and_ReturnIssuePrns()
        {
            var npwdGetIssuedPrnsResponse = _fixture.Create<GetPrnsResponseModel>();
            var jsonResponse = JsonConvert.SerializeObject(npwdGetIssuedPrnsResponse);
            
            var filter = "1 eq 1";
            var baseUrl = "http://localhost";
            var expectedRequestUri = new Uri($"{baseUrl}/oData/PRNs?$filter={filter}");

            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns(baseUrl);

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(baseUrl)
            };

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(HttpClientNames.Npwd))
                .Returns(httpClient);

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _mockLogger.Object);

            var result = await _npwdClient.GetIssuedPrns(filter);
            // Act & Assert
            httpMessageHandlerMock.Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(
                    req => req.Method == HttpMethod.Get
                           && req.RequestUri == expectedRequestUri),
                ItExpr.IsAny<CancellationToken>());

            result.Should().BeEquivalentTo(npwdGetIssuedPrnsResponse.Value);
        }

        [Fact]
        public async Task GetIssuedPrns_ShouldThowException_if_npwdRequest_fails()
        {
            var filter = "1 eq 1";
            var baseUrl = "http://localhost";

            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns(baseUrl);

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

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _configurationServiceMock.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _npwdClient.GetIssuedPrns(filter));
        }

    }
}