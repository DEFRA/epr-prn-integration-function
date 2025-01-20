using Moq;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.CommonService;
using EprPrnIntegration.Common.UnitTests.Helpers;
using EprPrnIntegration.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using System.Text.Json;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.CommonService
{
    public class CommonDataServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ILogger<CommonDataService>> _loggerMock;
        private readonly Mock<IOptions<Configuration.Service>> _mockConfig;

        public CommonDataServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<CommonDataService>>();
            _mockConfig = new Mock<IOptions<Configuration.Service>>();

            _mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                CommonDataServiceBaseUrl = "http://localhost:5001/",
                CommonDataServiceEndPointName = "api/commondata"
            });
        }

        private CommonDataService CreateCommonDataService(string responseContent = "", System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var mockHandler = new MockHttpMessageHandler(responseContent, statusCode);
            var httpClient = new HttpClient(mockHandler);
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            return new CommonDataService(
                _mockHttpContextAccessor.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _mockConfig.Object
            );
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldReturnCorrectData()
        {
            // Arrange
            var mockData = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse { OrganisationName = "Org1", TradingName = "Trade1", OrganisationId = "1" },
                new UpdatedProducersResponse { OrganisationName = "Org2", TradingName = "Trade2", OrganisationId = "2" }
            };
            var mockDataJson = JsonSerializer.Serialize(mockData);
            var commonDataService = CreateCommonDataService(mockDataJson);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            var result = await commonDataService.GetUpdatedProducers(fromDate, toDate, cancellationToken);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().HaveCount(2);
            result[0].OrganisationName.Should().Be("Org1");
            result[1].OrganisationId.Should().Be("2");
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldLogInformation_WhenCalled()
        {
            // Arrange
            var mockDataJson = JsonSerializer.Serialize(new List<UpdatedProducersResponse>());
            var commonDataService = CreateCommonDataService(mockDataJson);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            await commonDataService.GetUpdatedProducers(fromDate, toDate, cancellationToken);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Getting updated producers list.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldReturnEmptyList_WhenNoProducersFound()
        {
            // Arrange
            var commonDataService = CreateCommonDataService("[]");

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            var result = await commonDataService.GetUpdatedProducers(fromDate, toDate, cancellationToken);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldThrowException_WhenApiReturnsError()
        {
            // Arrange
            var commonDataService = CreateCommonDataService("", System.Net.HttpStatusCode.InternalServerError);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act & Assert
            await Assert.ThrowsAsync<ResponseCodeException>(() => commonDataService.GetUpdatedProducers(fromDate, toDate, cancellationToken));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenBaseUrlIsMissing()
        {
            // Arrange
            _mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                CommonDataServiceBaseUrl = null,
                CommonDataServiceEndPointName = "api/commondata"
            });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommonDataService(
                    _mockHttpContextAccessor.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    _loggerMock.Object,
                    _mockConfig.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenEndPointNameIsMissing()
        {
            // Arrange
            _mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                CommonDataServiceBaseUrl = "http://localhost:5001/",
                CommonDataServiceEndPointName = null
            });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommonDataService(
                    _mockHttpContextAccessor.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    _loggerMock.Object,
                    _mockConfig.Object));
        }
    }
}