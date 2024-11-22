using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Service;
using Moq;
using Moq.Protected;
using System.Net;

namespace EprPrnIntegration.Common.Tests.Client
{
    public class NpwdClientTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfigurationService> _configurationServiceMock;
        private readonly HttpClient _httpClient;
        private NpwdClient _npwdClient;

        public NpwdClientTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationServiceMock = new Mock<IConfigurationService>();

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

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _configurationServiceMock.Object);
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
                    NPWDCode = "NPWD001",
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

            _npwdClient = new NpwdClient(_httpClientFactoryMock.Object, _configurationServiceMock.Object);
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
    }
}