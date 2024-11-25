using Moq;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using EprPrnIntegration.Common.UnitTests.Helpers;
using EprPrnIntegration.Common.Exceptions;

namespace EprPrnIntegration.Common.UnitTests.PrnBackendService
{
    public class PrnServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IOptions<Configuration.Service>> _mockConfig;
        private readonly MockLogger<PrnService> _mockLogger;

        public PrnServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockLogger = new MockLogger<PrnService>();
            _mockConfig = new Mock<IOptions<Configuration.Service>>();

            _mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                PrnBaseUrl = "http://localhost:5575/",
                PrnEndPointName = "api/v1/prn"
            });
        }

        private PrnService CreatePrnService(string responseContent = "", System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var mockHandler = new MockHttpMessageHandler(responseContent, statusCode);
            var httpClient = new HttpClient(mockHandler);
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            return new PrnService(
                _mockHttpContextAccessor.Object,
                httpClientFactoryMock,
                _mockLogger,
                _mockConfig.Object
            );
        }

        [Fact]
        public async Task GetUpdatedPrns_ShouldReturnCorrectData()
        {
            // Arrange
            var mockData = new List<UpdatedPrnsResponseModel>
                {
                    new UpdatedPrnsResponseModel { EvidenceNo = "001", EvidenceStatusCode = "Active", StatusDate = DateTime.Now },
                    new UpdatedPrnsResponseModel { EvidenceNo = "002", EvidenceStatusCode = "Inactive", StatusDate = DateTime.Now }
                };

            var mockDataJson = JsonSerializer.Serialize(mockData);
            var _prnService1 = CreatePrnService(mockDataJson);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _prnService1.GetUpdatedPrns(fromDate, toDate, cancellationToken);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("001", result[0].EvidenceNo);
            Assert.Equal("Active", result[0].EvidenceStatusCode);
            Assert.Equal("002", result[1].EvidenceNo);
            Assert.Equal("Inactive", result[1].EvidenceStatusCode);
        }

        [Fact]
        public async Task GetUpdatedPrns_ShouldLogInformationWhenGettingPrns()
        {
            // Arrange
            var mockData = new List<UpdatedPrnsResponseModel>
            {
                new UpdatedPrnsResponseModel { EvidenceNo = "001", EvidenceStatusCode = "Active", StatusDate = DateTime.Now }
            };
            var mockDataJson = JsonSerializer.Serialize(mockData);
            var _prnService1 = CreatePrnService(mockDataJson);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            await _prnService1.GetUpdatedPrns(fromDate, toDate, cancellationToken);

            // Assert
            Assert.Contains("Getting updated PRN's.", _mockLogger.LogMessages);
        }

        [Fact]
        public async Task GetUpdatedPrns_ShouldReturnEmptyList_WhenNoPrnsReturned()
        {
            // Arrange
            var _prnService1 = CreatePrnService("[]");

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _prnService1.GetUpdatedPrns(fromDate, toDate, cancellationToken);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUpdatedPrns_ShouldThrowException_WhenBaseHttpServiceThrowsException()
        {
            // Arrange
            var _prnService1 = CreatePrnService("", System.Net.HttpStatusCode.BadRequest);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Assert
            var exception = await Assert.ThrowsAsync<ResponseCodeException>(() => _prnService1.GetUpdatedPrns(fromDate, toDate, cancellationToken));
            Assert.Contains("EprPrnIntegration.Common.Exceptions.ResponseCodeException", exception.Message);
        }
    }
}
