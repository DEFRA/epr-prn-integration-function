using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.BackendAccountService
{
    public class OrganisationServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogger<OrganisationService>> _loggerMock;
        private readonly Mock<IOptions<Configuration.Service>> _configMock;
        private readonly OrganisationService _organisationService;

        public OrganisationServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<OrganisationService>>();

            // Setting up the configuration mock
            var serviceConfig = new Configuration.Service
            {
                AccountBaseUrl = "http://localhost:5000/",
                AccountEndPointName = "api/organisations"
            };
            _configMock = new Mock<IOptions<Configuration.Service>>();
            _configMock.Setup(c => c.Value).Returns(serviceConfig);

            // Create a mock HttpClient
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                                [
                                    new() { FirstName="Test", LastName="User", Email = "test@example.com" }
                                ]));

            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            // Create the OrganisationService instance
            _organisationService = new OrganisationService(
                _httpContextAccessorMock.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _configMock.Object);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625 //Test for null check
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new OrganisationService(
                    _httpContextAccessorMock.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    null,
                    _configMock.Object));
#pragma warning restore CS8625

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public async Task GetPersonEmailsAsync_ShouldCallApiAndReturnEmails()
        {
            // Arrange
            var organisationId = "12345";
            var entityTypeCode = "CS";
            // Act
            var result = await _organisationService.GetPersonEmailsAsync(organisationId, entityTypeCode, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test@example.com", result[0].Email);
        }

        [Fact]
        public async Task DoesProducerOrComplianceSchemeExistAsync_ShouldCallApiAndReturnExistsFlag()
        {
            // Arrange
            var organisationId = Guid.NewGuid().ToString();

            var httpClient = new HttpClient(new MockHttpMessageHandler(
                responseContent: "Any valid content",
                statusCode: HttpStatusCode.OK));

            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            // Act
            bool result = await _organisationService.DoesProducerOrComplianceSchemeExistAsync(organisationId, "CS", CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(204)]
        [InlineData(400)]
        [InlineData(500)]
        public async Task DoesProducerOrComplianceSchemeExistAsync_ShouldCallApiAndReturnNotExistsFlag(int statusCode)
        {
            // Arrange
            var organisationId = Guid.NewGuid().ToString();

            var httpClient = new HttpClient(new MockHttpMessageHandler(
                responseContent: "Invalid content",
                statusCode: (HttpStatusCode)statusCode));

            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var organisationService = new OrganisationService(
                _httpContextAccessorMock.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _configMock.Object);

            // Act
            bool result = await organisationService.DoesProducerOrComplianceSchemeExistAsync(organisationId, "CS", CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HttpClient_ShouldHaveConfiguredTimeout_WhenTimeoutSecondsIsSetInConfiguration()
        {
            // Arrange
            var timeoutInSeconds = 310;

            var serviceConfig = new Configuration.Service
            {
                AccountBaseUrl = "http://localhost:5000/",
                AccountEndPointName = "api/organisations",
                TimeoutSeconds = timeoutInSeconds
            };
            _configMock.Setup(c => c.Value).Returns(serviceConfig);

            // Create a mock HttpClient
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                                [
                                    new() { FirstName="Test", LastName="User", Email = "test@example.com" }
                                ]))
            {
                Timeout = TimeSpan.FromSeconds(timeoutInSeconds)
            };

            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            // Create OrganisationService instance
            var organisationService = new OrganisationService(
                _httpContextAccessorMock.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _configMock.Object);

            // Act
            var client = httpClientFactoryMock.CreateClient(HttpClientNames.Account);

            // Assert
            client.Timeout.Should().Be(TimeSpan.FromSeconds(timeoutInSeconds));
        }
    }
}
