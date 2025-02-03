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
                    Status ="DR Registered",
                    OrganisationType =  "DR",
                    BusinessCountry = "Northern Ireland"
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
            Assert.Equal("Northern Ireland Environment Agency", producer.Agency);
        }

        [Fact]
        public void GetStatusCode_DRRegisteredAndDR_ReturnsPRRegistered()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                 new UpdatedProducersResponse
                {
                    Status = "DR Registered",
                    OrganisationType = "DR"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);
            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-REGISTERED", producer.StatusCode);
            Assert.Equal("Environment Agency", producer.Agency);
        }

        [Fact]
        public void GetStatusCode_DRDeletedAndDR_ReturnsPRCancelled()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = "DR Deleted",
                    OrganisationType = "DR"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-CANCELLED", producer.StatusCode);
        }

        [Fact]
        public void GetStatusCode_CSDeletedAndDR_ReturnsPRCancelled()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = "CSO Deleted",
                    OrganisationType = "DR"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-CANCELLED", producer.StatusCode);
        }

        [Fact]
        public void GetStatusCode_DRMovedToCSAndCSM_ReturnsPRRegistered()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = "DR Moved to CS",
                    OrganisationType = "CSM"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-REGISTERED", producer.StatusCode);
        }

        [Fact]
        public void GetStatusCode_NotAMemberOfCSAndDR_ReturnsPRRegistered()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
            {
                Status = "Not a Member of CS",
                OrganisationType = "DR"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-REGISTERED", producer.StatusCode);
        }

        [Fact]
        public void GetStatusCode_CSAddedAndS_ReturnsPRRegistered()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = "CS Added",
                    OrganisationType = "S"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-REGISTERED", producer.StatusCode);
        }

        [Fact]
        public void GetStatusCode_CSDeletedAndS_ReturnsPRCancelled()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = "CS Deleted",
                    OrganisationType = "S"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal("PR-CANCELLED", producer.StatusCode);
        }

        [Fact]
        public void GetStatusCode_UnmatchedStatusAndOrganisationType_ReturnsEmpty()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponse>
            {
                new UpdatedProducersResponse
                {
                    Status = "Some Unmatched Status",
                    OrganisationType = "Some Organisation Type"
                }
            };

            // Act
            var result = ProducerMapper.Map(updatedProducers, _configurationMock.Object);

            // Assert
            var producer = result.Value[0];
            Assert.Equal(string.Empty, producer.StatusCode);
        }
    }
}