using System.Net;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.WasteOrganisationsService
{
    public class WasteOrganisationsServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IOptions<WasteOrganisationsApiConfiguration>> _mockConfig;
        private readonly Mock<
            ILogger<Common.RESTServices.WasteOrganisationsService.WasteOrganisationsService>
        > _loggerMock;

        public WasteOrganisationsServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _loggerMock =
                new Mock<
                    ILogger<Common.RESTServices.WasteOrganisationsService.WasteOrganisationsService>
                >();
            _mockConfig = new Mock<IOptions<WasteOrganisationsApiConfiguration>>();

            _mockConfig
                .Setup(c => c.Value)
                .Returns(
                    new WasteOrganisationsApiConfiguration
                    {
                        BaseUrl = "http://localhost:5575/",
                        ClientId = "test-client-id",
                        ClientSecret = "test-secret",
                        AccessTokenUrl = "http://localhost:5575/token",
                        TimeoutSeconds = 30,
                    }
                );
        }

        private Common.RESTServices.WasteOrganisationsService.WasteOrganisationsService CreateWasteOrganisationsService(
            string responseContent = "",
            HttpStatusCode statusCode = HttpStatusCode.OK
        )
        {
            var mockHandler = new MockHttpMessageHandler(responseContent, statusCode);
            var httpClient = new HttpClient(mockHandler);
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            return new Common.RESTServices.WasteOrganisationsService.WasteOrganisationsService(
                _mockHttpContextAccessor.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _mockConfig.Object
            );
        }

        [Fact]
        public async Task GetOrganisation_ShouldReturnOkResponse_WhenOrganisationExists()
        {
            // Arrange
            var organisationId = "test-org-123";
            var responseContent = "{\"id\": \"test-org-123\", \"name\": \"Test Organisation\"}";
            var sut = CreateWasteOrganisationsService(responseContent, HttpStatusCode.OK);

            // Act
            var result = await sut.GetOrganisation(organisationId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task GetOrganisation_ShouldLogInformation_WhenGettingOrganisation()
        {
            // Arrange
            var organisationId = "test-org-123";
            var responseContent = "{\"id\": \"test-org-123\"}";
            var sut = CreateWasteOrganisationsService(responseContent, HttpStatusCode.OK);

            // Act
            await sut.GetOrganisation(organisationId, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                logger =>
                    logger.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) =>
                                ContainsString(v, "Getting organisation details for")
                                && ContainsString(v, organisationId)
                        ),
                        null,
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetOrganisation_ShouldCallCorrectEndpoint()
        {
            // Arrange
            var organisationId = "test-org-456";
            var mockHandler = new MockHttpMessageHandler("", HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler);
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var sut = new Common.RESTServices.WasteOrganisationsService.WasteOrganisationsService(
                _mockHttpContextAccessor.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _mockConfig.Object
            );

            // Act
            await sut.GetOrganisation(organisationId, CancellationToken.None);

            // Assert
            Assert.NotNull(mockHandler.LastRequest);
            Assert.Contains(
                $"organisations/{organisationId}",
                mockHandler.LastRequest.RequestUri?.ToString()
            );
        }

        [Fact]
        public async Task GetOrganisation_ReturnsNotFound_WhenApiReturnsNotFound()
        {
            // Arrange
            var organisationId = "non-existent-org";
            var sut = CreateWasteOrganisationsService("", HttpStatusCode.NotFound);

            // Act & Assert
            (await sut.GetOrganisation(organisationId, CancellationToken.None))
                .StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetOrganisation_ShouldWorkWithEmptyResponse()
        {
            // Arrange
            var organisationId = "test-org-123";
            var sut = CreateWasteOrganisationsService("", HttpStatusCode.OK);

            // Act
            var result = await sut.GetOrganisation(organisationId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        private static bool ContainsString(object obj, string value)
        {
            return obj?.ToString()?.Contains(value) == true;
        }
    }
}
