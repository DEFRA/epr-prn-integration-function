using System.Net;
using System.Text.Json;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService;
using EprPrnIntegration.Common.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.PrnBackendService
{
    public class PrnServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IOptions<Configuration.Service>> _mockConfig;
        private readonly Mock<ILogger<PrnService>> _loggerMock;

        public PrnServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<PrnService>>();
            _mockConfig = new Mock<IOptions<Configuration.Service>>();

            _mockConfig
                .Setup(c => c.Value)
                .Returns(new Configuration.Service { PrnBaseUrl = "http://localhost:5575/" });
        }

        [Fact]
        public async Task SavePrn_ShouldCallServiceWithCorrectRequest()
        {
            // Arrange
            var organisationId = Guid.NewGuid();
            var statusUpdatedOn = DateTime.UtcNow;
            var request = CreateSavePrnDetailsRequest(
                organisationId: organisationId,
                statusUpdatedOn: statusUpdatedOn
            );
            var (sut, mockHandler) = CreatePrnServiceV2();

            // Act
            await sut.SavePrn(request, CancellationToken.None);

            // Assert
            Assert.NotNull(mockHandler.LastRequest);
            Assert.Equal(HttpMethod.Post, mockHandler.LastRequest.Method);
            Assert.Contains("api/v2/prn", mockHandler.LastRequest.RequestUri?.ToString());

            // Verify the payload
            Assert.NotNull(mockHandler.LastRequestContent);
            var sentRequest = JsonSerializer.Deserialize<SavePrnDetailsRequest>(
                mockHandler.LastRequestContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            sentRequest.Should().BeEquivalentTo(request);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task SavePrn_ShouldReturnStatusCode_WhenPostFails(HttpStatusCode statusCode)
        {
            var request = CreateSavePrnDetailsRequest();
            var (sut, _) = CreatePrnServiceV2("", statusCode);

            var response = await sut.SavePrn(request, CancellationToken.None);
            response.StatusCode.Should().Be(statusCode);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenPrnBaseUrlIsNull()
        {
            // Arrange
            var mockConfig = new Mock<IOptions<Configuration.Service>>();
            mockConfig.Setup(c => c.Value).Returns(new Configuration.Service { PrnBaseUrl = null });

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PrnService(
                    _mockHttpContextAccessor.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    _loggerMock.Object,
                    mockConfig.Object
                )
            );

            Assert.Contains("PrnService BaseUrl configuration is missing", exception.Message);
        }

        private (PrnService service, MockHttpMessageHandler handler) CreatePrnServiceV2(
            string responseContent = "",
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK
        )
        {
            var mockHandler = new MockHttpMessageHandler(responseContent, statusCode);
            var httpClient = new HttpClient(mockHandler);
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var service = new PrnService(
                _mockHttpContextAccessor.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _mockConfig.Object
            );

            return (service, mockHandler);
        }

        private SavePrnDetailsRequest CreateSavePrnDetailsRequest(
            Guid? organisationId = null,
            DateTime? statusUpdatedOn = null
        )
        {
            return new SavePrnDetailsRequest
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
                ObligationYear = "2024",
            };
        }
    }
}
