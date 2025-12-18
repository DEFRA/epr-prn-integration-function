using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService;
using EprPrnIntegration.Common.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.PrnBackendService
{
    public class PrnServiceV2Tests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IOptions<Configuration.Service>> _mockConfig;
        private readonly Mock<ILogger<PrnServiceV2>> _loggerMock;

        public PrnServiceV2Tests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<PrnServiceV2>>();
            _mockConfig = new Mock<IOptions<Configuration.Service>>();

            _mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                PrnBaseUrl = "http://localhost:5575/",
                PrnEndPointNameV2 = "api/v2/prn"
            });
        }
       
        [Fact]
        public async Task SavePrn_ShouldCallServiceWithCorrectRequest()
        {
            // Arrange
            var organisationId = Guid.NewGuid();
            var statusUpdatedOn = DateTime.UtcNow;
            var request = CreateSavePrnDetailsRequest(
                organisationId: organisationId,
                statusUpdatedOn: statusUpdatedOn);
            var (sut, mockHandler) = CreatePrnServiceV2();

            // Act
            await sut.SavePrn(request);

            // Assert
            Assert.NotNull(mockHandler.LastRequest);
            Assert.Equal(HttpMethod.Post, mockHandler.LastRequest.Method);
            Assert.Contains("api/v2/prn", mockHandler.LastRequest.RequestUri?.ToString());

            // Verify the payload
            Assert.NotNull(mockHandler.LastRequestContent);
            var sentRequest = JsonSerializer.Deserialize<SavePrnDetailsRequestV2>(mockHandler.LastRequestContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            sentRequest.Should().BeEquivalentTo(request);
        }

        [Fact]
        public async Task SavePrn_ShouldThrowException_WhenPostFails()
        {
            // Arrange
            var request = CreateSavePrnDetailsRequest();
            var (sut, _) = CreatePrnServiceV2("", System.Net.HttpStatusCode.BadRequest);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(() => sut.SavePrn(request));
        }

        [Fact]
        public async Task SavePrn_ShouldThrowException_WhenServerError()
        {
            // Arrange
            var request = CreateSavePrnDetailsRequest();
            var (sut, _) = CreatePrnServiceV2("", System.Net.HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(() => sut.SavePrn(request));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenPrnBaseUrlIsNull()
        {
            // Arrange
            var mockConfig = new Mock<IOptions<Configuration.Service>>();
            mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                PrnBaseUrl = null,
                PrnEndPointNameV2 = "api/v2/prn"
            });

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PrnServiceV2(
                    _mockHttpContextAccessor.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    _loggerMock.Object,
                    mockConfig.Object
                )
            );

            Assert.Contains("PrnService BaseUrl configuration is missing", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenPrnEndPointNameV2IsNull()
        {
            // Arrange
            var mockConfig = new Mock<IOptions<Configuration.Service>>();
            mockConfig.Setup(c => c.Value).Returns(new Configuration.Service
            {
                PrnBaseUrl = "http://localhost:5575/",
                PrnEndPointNameV2 = null
            });

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PrnServiceV2(
                    _mockHttpContextAccessor.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    _loggerMock.Object,
                    mockConfig.Object
                )
            );

            Assert.Contains("PrnService EndPointNameV2 configuration is missing", exception.Message);
        }
        
        private (PrnServiceV2 service, MockHttpMessageHandler handler) CreatePrnServiceV2(string responseContent = "", System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var mockHandler = new MockHttpMessageHandler(responseContent, statusCode);
            var httpClient = new HttpClient(mockHandler);
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var service = new PrnServiceV2(
                _mockHttpContextAccessor.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _mockConfig.Object
            );

            return (service, mockHandler);
        }

        private SavePrnDetailsRequestV2 CreateSavePrnDetailsRequest(
            Guid? organisationId = null,
            DateTime? statusUpdatedOn = null)
        {
            return new SavePrnDetailsRequestV2
            {
                SourceSystemId = "RREPW",
                PrnNumber = "PRN-1234",
                PrnStatusId = 1,
                PrnSignatory = "John Doe",
                StatusUpdatedOn = statusUpdatedOn ?? DateTime.UtcNow,
                IssuedByOrg = "Org A",
                OrganisationId = organisationId ?? Guid.NewGuid(),
                OrganisationName = "Org Name",
                AccreditationNumber = "ACC-123",
                AccreditationYear = "2024",
                MaterialName = "Plastic",
                ReprocessorExporterAgency = "Agency",
                DecemberWaste = false,
                IsExport = false,
                TonnageValue = 100,
                ProcessToBeUsed = "Recycling",
                ObligationYear = "2024"
            };
        }
    }
}