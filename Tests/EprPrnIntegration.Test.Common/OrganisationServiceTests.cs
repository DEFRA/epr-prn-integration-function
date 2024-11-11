using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Test.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Test.Common
{
    public class OrganisationServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogger<OrganisationService>> _loggerMock;
        private readonly Mock<IOptions<Service>> _configMock;
        private readonly OrganisationService _organisationService;

        public OrganisationServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<OrganisationService>>();

            // Setting up the configuration mock
            var serviceConfig = new Service
            {
                Url = "http://localhost:5000/",
                EndPointName = "api/organisations"
            };
            _configMock = new Mock<IOptions<Service>>();
            _configMock.Setup(c => c.Value).Returns(serviceConfig);

            // Create a mock HttpClient
            var httpClient = new HttpClient(new FakeHttpMessageHandler(new List<PersonEmail>
                                {
                                    new PersonEmail { Email = "test@example.com" }
                                }));

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
            // Arrange
            ILogger<OrganisationService> logger = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new OrganisationService(
                    _httpContextAccessorMock.Object,
                    new HttpClientFactoryMock(new HttpClient()),
                    logger,
                    _configMock.Object));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public async Task GetPersonEmailsAsync_ShouldCallApiAndReturnEmails()
        {
            // Arrange
            var organisationId = "12345";

            // Act
            var result = await _organisationService.GetPersonEmailsAsync(organisationId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test@example.com", result[0].Email);
        }
    }
}
