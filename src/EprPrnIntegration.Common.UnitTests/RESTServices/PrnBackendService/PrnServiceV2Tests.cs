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
                PrnEndPointNameV2 = "api/v2"
            });
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

        [Fact]
        public async Task SavePrn_ShouldCallServiceWithCorrectRequest()
        {
            // Arrange
            var organisationId = Guid.NewGuid();
            var statusUpdatedOn = DateTime.UtcNow;
            var request = new SavePrnDetailsRequestV2
            {
                SourceSystemId = "RREPW",
                PrnNumber = "PRN-1234",
                PrnStatusId = 1,
                PrnSignatory = "John Doe",
                StatusUpdatedOn = statusUpdatedOn,
                IssuedByOrg = "Org A",
                OrganisationId = organisationId,
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
            Assert.Equal("RREPW", sentRequest.SourceSystemId);
            Assert.Equal("PRN-1234", sentRequest.PrnNumber);
            Assert.Equal(1, sentRequest.PrnStatusId);
            Assert.Equal("John Doe", sentRequest.PrnSignatory);
            Assert.Equal("Org A", sentRequest.IssuedByOrg);
            Assert.Equal(organisationId, sentRequest.OrganisationId);
            Assert.Equal("Org Name", sentRequest.OrganisationName);
            Assert.Equal("ACC-123", sentRequest.AccreditationNumber);
            Assert.Equal("2024", sentRequest.AccreditationYear);
            Assert.Equal("Plastic", sentRequest.MaterialName);
            Assert.Equal("Agency", sentRequest.ReprocessorExporterAgency);
            Assert.False(sentRequest.DecemberWaste);
            Assert.False(sentRequest.IsExport);
            Assert.Equal(100, sentRequest.TonnageValue);
            Assert.Equal("Recycling", sentRequest.ProcessToBeUsed);
            Assert.Equal("2024", sentRequest.ObligationYear);
        }

        [Fact]
        public async Task SavePrn_ShouldThrowException_WhenPostFails()
        {
            // Arrange
            var request = new SavePrnDetailsRequestV2
            {
                SourceSystemId = "RREPW",
                PrnNumber = "PRN-ERROR",
                PrnStatusId = 1,
                PrnSignatory = "Test User",
                StatusUpdatedOn = DateTime.UtcNow,
                IssuedByOrg = "Org C",
                OrganisationId = Guid.NewGuid(),
                OrganisationName = "Org Name C",
                AccreditationNumber = "ACC-789",
                AccreditationYear = "2024",
                MaterialName = "Paper",
                ReprocessorExporterAgency = "Agency C",
                DecemberWaste = false,
                IsExport = false,
                TonnageValue = 50,
                ProcessToBeUsed = "Recycling",
                ObligationYear = "2024"
            };
            var (sut, _) = CreatePrnServiceV2("", System.Net.HttpStatusCode.BadRequest);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(() => sut.SavePrn(request));
        }

        [Fact]
        public async Task SavePrn_ShouldThrowException_WhenServerError()
        {
            // Arrange
            var request = new SavePrnDetailsRequestV2
            {
                SourceSystemId = "RREPW",
                PrnNumber = "PRN-SERVER-ERROR",
                PrnStatusId = 1,
                PrnSignatory = "Test User",
                StatusUpdatedOn = DateTime.UtcNow,
                IssuedByOrg = "Org D",
                OrganisationId = Guid.NewGuid(),
                OrganisationName = "Org Name D",
                AccreditationNumber = "ACC-999",
                AccreditationYear = "2024",
                MaterialName = "Metal",
                ReprocessorExporterAgency = "Agency D",
                DecemberWaste = true,
                IsExport = true,
                TonnageValue = 150,
                ProcessToBeUsed = "Export",
                ObligationYear = "2024"
            };
            var (sut, _) = CreatePrnServiceV2("", System.Net.HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(() => sut.SavePrn(request));
        }
    }
}
