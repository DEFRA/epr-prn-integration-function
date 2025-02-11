using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class ProducerMapperTests
    {
        private readonly Mock<IConfiguration> _configurationMock;

        public ProducerMapperTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["ProducersContext"]).Returns("https://fat.npwd.org.uk/odata/Producers/$delta");
        }

        [Fact]
        public void MapToDelta_NullInput_ReturnsEmptyProducerDelta()
        {
            // Act
            var result = ProducerMapper.Map(null!, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Context);
            Assert.Equal("https://fat.npwd.org.uk/odata/Producers/$delta", result.Context);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public void MapToDelta_EmptyList_ReturnsEmptyProducerDelta()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>();

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Context);
            Assert.Equal("https://fat.npwd.org.uk/odata/Producers/$delta", result.Context);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Theory]
        [InlineData("DR Registered", "DR", "PR-REGISTERED")]
        [InlineData("DR Deleted", "DR", "PR-CANCELLED")]
        [InlineData("CSO Deleted", "DR", "PR-CANCELLED")]
        [InlineData("DR Moved to CS", "CSM", "PR-REGISTERED")]
        [InlineData("Not a Member of CS", "DR", "PR-REGISTERED")]
        [InlineData("CS Added", "S", "CSR-REGISTERED")]
        [InlineData("CS Deleted", "S", "CSR-CANCELLED")]
        [InlineData("Some Unmatched Status", "Some Organisation Type", "")]
        public void MapToDelta_MapsCorrectStatusCode(string status, string orgType, string expectedStatusCode)
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = status,
                    OrganisationType = orgType
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal(expectedStatusCode, producer.StatusCode);
        }

        [Fact]
        public void MapToDelta_ValidInput_MapsToProducerDelta()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    CompaniesHouseNumber = "12345678",
                    AddressLine1 = "SubBuilding A",
                    AddressLine2 = "Building A",
                    Town = "Town A",
                    County = "County A",
                    Country = "Country A",
                    Postcode = "12345",
                    Status = "DR Registered",
                    OrganisationType = "DR",
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Context);
            Assert.Equal("https://fat.npwd.org.uk/odata/Producers/$delta", result.Context);
            Assert.Single(result.Value);

            var producer = result.Value[0];
            Assert.Equal("12345678", producer.CompanyRegNo);
            Assert.Equal("SubBuilding A", producer.AddressLine1);
            Assert.Equal("Building A", producer.AddressLine2);
            Assert.Equal("12345", producer.Postcode);
            Assert.Equal("PR-REGISTERED", producer.StatusCode);
            Assert.Equal("Registered", producer.StatusDesc);
        }
    }
}