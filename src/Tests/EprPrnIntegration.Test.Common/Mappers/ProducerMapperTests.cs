using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Test.Common.Mappers;

public class ProducerMapperTests
{
    [Fact]
    public void Map_NullInput_ReturnsEmptyList()
    {
        // Arrange
        List<UpdatedProducersResponseModel> updatedProducers = null;

        // Act
        var result = ProducerMapper.Map(updatedProducers);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Map_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var updatedProducers = new List<UpdatedProducersResponseModel>();

        // Act
        var result = ProducerMapper.Map(updatedProducers);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Map_ValidInput_MapsToProducerList()
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
                ExternalId = "EXT001",
                StatusCode = "Active",
                StatusDesc = "Active Description",
                StatusDate = "2024-01-01"
            }
        };

        // Act
        var result = ProducerMapper.Map(updatedProducers);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var producer = result[0];
        Assert.Equal("Producer A", producer.ProducerName);
        Assert.Equal("12345678", producer.CompanyRegNo);
        Assert.Equal("SubBuilding A 10 Building A", producer.AddressLine1);
        Assert.Equal("Street A", producer.AddressLine2);
        Assert.Equal("Locality A", producer.AddressLine3);
        Assert.Equal("Dependent Locality A", producer.AddressLine4);
        Assert.Equal("Town A", producer.Town);
        Assert.Equal("County A", producer.County);
        Assert.Equal("Country A", producer.Country);
        Assert.Equal("12345", producer.Postcode);
        Assert.Equal("CS", producer.EntityTypeCode);
        Assert.Equal("Compliance Scheme", producer.EntityTypeName);
        Assert.Equal("REF001", producer.NPWDCode);
        Assert.Equal("EXT001", producer.EPRId);
        Assert.Equal("REF001", producer.EPRCode);
        Assert.Equal("Active", producer.StatusCode);
        Assert.Equal("Active Description", producer.StatusDesc);
        Assert.Equal("2024-01-01", producer.StatusDate);
    }

    [Fact]
    public void Map_IsComplianceSchemeFalse_SetsEntityTypeCorrectly()
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
        var result = ProducerMapper.Map(updatedProducers);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var producer = result[0];
        Assert.Equal("Producer B", producer.ProducerName);
        Assert.Equal("DR", producer.EntityTypeCode);
        Assert.Equal("Direct Registrant", producer.EntityTypeName);
    }
}