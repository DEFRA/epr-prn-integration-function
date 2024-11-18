using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.RESTServices.NpwdService;
using EprPrnIntegration.Common.Service;
using Moq.Protected;
using Moq;
using System.Net;

namespace EprPrnIntegration.Test.Common.RESTServices.NpwdService
{
    public class ProducerServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfigurationService> _configurationServiceMock;
        private readonly HttpClient _httpClient;
        private ProducerService _producerService;

        public ProducerServiceTests()
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

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(EprPrnIntegration.Common.Constants.HttpClientNames.Npwd))
                .Returns(_httpClient);

            _producerService = new ProducerService(_httpClientFactoryMock.Object, _configurationServiceMock.Object);
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
            var response = await _producerService.UpdateProducerList(updatedProducers);

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

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(EprPrnIntegration.Common.Constants.HttpClientNames.Npwd))
                .Returns(httpClient);

            _configurationServiceMock.Setup(service => service.GetNpwdApiBaseUrl())
                .Returns("http://localhost");

            _producerService = new ProducerService(_httpClientFactoryMock.Object, _configurationServiceMock.Object);
            // Act
            var response = await _producerService.UpdateProducerList(new List<Producer>());

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
            await Assert.ThrowsAsync<UriFormatException>(() => _producerService.UpdateProducerList(new List<Producer>()));
        }
    }
}