using Moq;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using EprPrnIntegration.Common.UnitTests.Helpers;
using EprPrnIntegration.Common.Exceptions;
using Microsoft.Extensions.Logging;
using AutoFixture;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.PrnBackendService
{
    public class PrnServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IOptions<Configuration.Service>> _mockConfig;
        private readonly Mock<ILogger<PrnService>> _loggerMock;
        private readonly Fixture _fixture = new();
        public PrnServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<PrnService>>();
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
                _loggerMock.Object,
                _mockConfig.Object
            );
        }

        [Fact]
        public async Task GetUpdatedPrns_ShouldReturnCorrectData()
        {
            // Arrange
            var mockData = new List<UpdatedPrnsResponseModel>
                {
                    new UpdatedPrnsResponseModel { EvidenceNo = "001", EvidenceStatusCode = "Active", StatusDate = new DateTime(2024, 12, 4, 15, 57, 2)  },
                    new UpdatedPrnsResponseModel { EvidenceNo = "002", EvidenceStatusCode = "Inactive", StatusDate = new DateTime(2024, 11, 3, 23, 51, 2)  }
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
                new UpdatedPrnsResponseModel { EvidenceNo = "001", EvidenceStatusCode = "Active", StatusDate = new DateTime(2024, 12, 4, 15, 57, 2) }
            };
            var mockDataJson = JsonSerializer.Serialize(mockData);
            var _prnService1 = CreatePrnService(mockDataJson);

            var fromDate = DateTime.Now.AddDays(-1);
            var toDate = DateTime.Now;
            var cancellationToken = new CancellationToken();

            // Act
            await _prnService1.GetUpdatedPrns(fromDate, toDate, cancellationToken);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Getting updated PRN's.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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

        [Fact]
        public async Task InsertPeprNpwdSyncPrns_LogsError_IfInvalidStatus()
        {
            var updatedPrns = _fixture.CreateMany<UpdatedPrnsResponseModel>();
            // Arrange
            var sut = CreatePrnService("", System.Net.HttpStatusCode.BadRequest);

            await sut.InsertPeprNpwdSyncPrns(updatedPrns);

            _loggerMock.VerifyLog(l => l.LogError(It.IsAny<InvalidDataException>(),It.Is<string>(s => s.Contains("Insert of sync data failed with ex:"))));
        }

        [Fact]
        public async Task InsertPeprNpwdSyncPrns_CallsService()
        {
            var updatedPrns = _fixture.Build<UpdatedPrnsResponseModel>().
                With(p => p.EvidenceStatusCode, "EV-ACCEP").CreateMany();
    
            var sut = CreatePrnService("", System.Net.HttpStatusCode.OK);

            await sut.InsertPeprNpwdSyncPrns(updatedPrns);
            _loggerMock.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Sync data iserted"))));
        }
    }
}