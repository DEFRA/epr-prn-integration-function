using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System.Net;

namespace EprPrnIntegration.Common.Tests.RESTServices.BackendAccountService
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
                Url = "http://localhost:5000/",
                EndPointName = "api/organisations"
            };
            _configMock = new Mock<IOptions<Configuration.Service>>();
            _configMock.Setup(c => c.Value).Returns(serviceConfig);

            // Create a mock HttpClient
            var httpClient = new HttpClient(new FakeHttpMessageHandler(new List<PersonEmail>
                                {
                                    new PersonEmail { FirstName="Test", LastName="User", Email = "test@example.com" }
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

            // Act
            var result = await _organisationService.GetPersonEmailsAsync(organisationId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test@example.com", result[0].Email);
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldCallApiAndReturnProducers()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;

            var expectedResponse = JsonConvert.SerializeObject(new List<UpdatedProducersResponseModel>
    {
        new UpdatedProducersResponseModel
        {
            ProducerName = "Producer A",
            CompaniesHouseNumber = "12345678",
            TradingName = "Trading A",
            ReferenceNumber = "REF001",
            Street = "Test Street",
            Town = "Test Town",
            Country = "Test Country",
            Postcode = "12345",
            OrganisationId = 1,
            OrganisationTypeId = 2,
            IsComplianceScheme = true,
            StatusCode = "Active",
            StatusDesc = "Active Description"
        },
        new UpdatedProducersResponseModel
        {
            ProducerName = "Producer B",
            CompaniesHouseNumber = "87654321",
            TradingName = "Trading B",
            ReferenceNumber = "REF002",
            Street = "Another Street",
            Town = "Another Town",
            Country = "Another Country",
            Postcode = "54321",
            OrganisationId = 2,
            OrganisationTypeId = 3,
            IsComplianceScheme = false,
            StatusCode = "Inactive",
            StatusDesc = "Inactive Description"
        }
    });

            var httpClient = new HttpClient(new MockHttpMessageHandler(expectedResponse));
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var organisationService = new OrganisationService(
                _httpContextAccessorMock.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _configMock.Object);

            // Act
            var result = await organisationService.GetUpdatedProducers(from, to, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            var firstProducer = result[0];
            Assert.Equal("Producer A", firstProducer.ProducerName);
            Assert.Equal("12345678", firstProducer.CompaniesHouseNumber);
            Assert.Equal("REF001", firstProducer.ReferenceNumber);

            var secondProducer = result[1];
            Assert.Equal("Producer B", secondProducer.ProducerName);
            Assert.Equal("87654321", secondProducer.CompaniesHouseNumber);
            Assert.Equal("REF002", secondProducer.ReferenceNumber);
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldReturnEmptyList_WhenApiReturnsEmpty()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;

            var httpClient = new HttpClient(new MockHttpMessageHandler("[]")); // Simulate empty response
            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var organisationService = new OrganisationService(
                _httpContextAccessorMock.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _configMock.Object);

            // Act
            var result = await organisationService.GetUpdatedProducers(from, to, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUpdatedProducers_ShouldThrowException_WhenApiFails()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;

            var httpClient = new HttpClient(new MockHttpMessageHandler(
                responseContent: "Internal Server Error",
                statusCode: HttpStatusCode.InternalServerError));

            var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

            var organisationService = new OrganisationService(
                _httpContextAccessorMock.Object,
                httpClientFactoryMock,
                _loggerMock.Object,
                _configMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ResponseCodeException>(() =>
                organisationService.GetUpdatedProducers(from, to, CancellationToken.None));
        }
    }
}
