using AutoFixture;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService;
using EprPrnIntegration.Common.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.PrnBackendService
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

            _loggerMock.VerifyLog(l => l.LogError(It.IsAny<InvalidDataException>(), It.Is<string>(s => s.Contains("Insert of sync data failed with ex:"))));
        }

        [Fact]
        public async Task InsertPeprNpwdSyncPrns_CallsService()
        {
            var updatedPrns = _fixture.Build<UpdatedPrnsResponseModel>().
                With(p => p.EvidenceStatusCode, "EV-ACCEP").CreateMany();

            var sut = CreatePrnService("", System.Net.HttpStatusCode.OK);

            await sut.InsertPeprNpwdSyncPrns(updatedPrns);
            _loggerMock.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Sync data inserted"))));
        }

        [Fact]
        public async Task SavePrn_ShouldCallServiceWithCorrectRequest()
        {
            // Arrange
            var request = new SavePrnDetailsRequest
            {
                EvidenceNo = "1234",
                EvidenceStatusCode = Enums.EprnStatus.ACCEPTED,
                StatusDate = DateTime.Now
            };
            var sut = CreatePrnService();

            // Act
            await sut.SavePrn(request);

            // Assert
            _loggerMock.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Saving PRN with id 1234"))), Times.Once);
        }

        [Fact]
        public async Task SavePrn_ShouldLogInformation_WhenSavingPrn()
        {
            // Arrange
            var request = new SavePrnDetailsRequest
            {
                EvidenceNo = "1234",
                EvidenceStatusCode = Enums.EprnStatus.ACCEPTED,
                StatusDate = DateTime.Now
            };
            var sut = CreatePrnService();

            // Act
            await sut.SavePrn(request);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Saving PRN with id 1234")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task SavePrn_ShouldThrowException_WhenPostFails()
        {
            // Arrange
            var request = new SavePrnDetailsRequest
            {
                EvidenceNo = "1234",
                EvidenceStatusCode = Enums.EprnStatus.ACCEPTED,
                StatusDate = DateTime.Now
            };
            var sut = CreatePrnService("", System.Net.HttpStatusCode.BadRequest);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(() => sut.SavePrn(request));
        }

        [Fact]
        public async Task GetReconsolidatedUpdatedPrns_ShouldReturnCorrectData()
        {
            // Arrange
            var mockData = new List<ReconcileUpdatedPrnsResponseModel>
    {
        new ReconcileUpdatedPrnsResponseModel
        {
            PrnNumber = "001",
            StatusName = "Approved",
            UpdatedOn = "2024-12-04T15:57:02",
            OrganisationName = "Company A"
        },
        new ReconcileUpdatedPrnsResponseModel
        {
            PrnNumber = "002",
            StatusName = "Rejected",
            UpdatedOn = "2024-12-03T23:51:02",
            OrganisationName = "Company B"
        }
    };

            var mockDataJson = JsonSerializer.Serialize(mockData);
            var sut = CreatePrnService(mockDataJson);

            // Act
            var result = await sut.GetReconsolidatedUpdatedPrns();

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("001", result[0].PrnNumber);
            Assert.Equal("Approved", result[0].StatusName);
            Assert.Equal("002", result[1].PrnNumber);
            Assert.Equal("Rejected", result[1].StatusName);
        }

        [Fact]
        public async Task GetReconsolidatedUpdatedPrns_ShouldReturnEmptyList_WhenNoDataExists()
        {
            // Arrange
            var sut = CreatePrnService("[]");

            // Act
            var result = await sut.GetReconsolidatedUpdatedPrns();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetReconsolidatedUpdatedPrns_ShouldLogInformation()
        {
            // Arrange
            var mockData = new List<ReconcileUpdatedPrnsResponseModel>
    {
        new ReconcileUpdatedPrnsResponseModel
        {
            PrnNumber = "001",
            StatusName = "Approved",
            UpdatedOn = "2024-12-04T15:57:02",
            OrganisationName = "Company A"
        }
    };

            var mockDataJson = JsonSerializer.Serialize(mockData);
            var sut = CreatePrnService(mockDataJson);

            // Act
            await sut.GetReconsolidatedUpdatedPrns();

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Getting reconsolidated updated PRN's.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task GetReconsolidatedUpdatedPrns_ShouldThrowException_WhenApiFails()
        {
            // Arrange
            var sut = CreatePrnService("", System.Net.HttpStatusCode.BadRequest);

            // Act & Assert
            await Assert.ThrowsAsync<ResponseCodeException>(() => sut.GetReconsolidatedUpdatedPrns());
        }
    }
}