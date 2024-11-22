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
            // Arrange
            List<UpdatedProducersResponseModel> updatedProducers = null;

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
        public void MapToDelta_EmptyList_ReturnsEmptyProducerDelta()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponseModel>();

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
            var updatedProducers = new List<UpdatedProducersResponseModel>
            {
                new UpdatedProducersResponseModel
                {
                    ProducerName = "Producer A",
                    CompaniesHouseNumber = "12345678",
                    SubBuildingName = "SubBuilding A",
                    BuildingNumber = "10",
                    BuildingName = "Building A",
                    Street = "Street A",
                    Locality = "Locality A",
                    DependentLocality = "Dependent Locality A",
                    Town = "Town A",
                    County = "County A",
                    Country = "Country A",
                    Postcode = "12345",
                    IsComplianceScheme = true,
                    ReferenceNumber = "REF001",
                    ExternalId = "EXT001"
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
            Assert.Equal("Producer A", producer.ProducerName);
            Assert.Equal("12345678", producer.CompanyRegNo);
            Assert.Equal("SubBuilding A 10 Building A", producer.AddressLine1);
            Assert.Equal("Street A", producer.AddressLine2);
            Assert.Equal("12345", producer.Postcode);
            Assert.Equal("CS", producer.EntityTypeCode);
            Assert.Equal("Compliance Scheme", producer.EntityTypeName);
            Assert.Equal("REF001", producer.EPRCode);
            Assert.Equal("EXT001", producer.EPRId);
        }

        [Fact]
        public void MapToDelta_IsComplianceSchemeFalse_SetsEntityTypeCorrectly()
        {
            // Arrange
            var updatedProducers = new List<UpdatedProducersResponseModel>
            {
                new UpdatedProducersResponseModel
                {
                    ProducerName = "Producer B",
                    IsComplianceScheme = false
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
            Assert.Equal("Producer B", producer.ProducerName);
            Assert.Equal("DR", producer.EntityTypeCode);
            Assert.Equal("Direct Registrant", producer.EntityTypeName);
        }
    }
}