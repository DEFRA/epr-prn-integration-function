namespace EprPrnIntegration.Common.Models;

public class UpdatedProducersResponseModel
{
    public string ProducerName { get; set; }
    public string CompaniesHouseNumber { get; set; }
    public string Name { get; set; }
    public string TradingName { get; set; }
    public string ReferenceNumber { get; set; }
    public string SubBuildingName { get; set; }
    public string BuildingName { get; set; }
    public string BuildingNumber { get; set; }
    public string Street { get; set; }
    public string Locality { get; set; }
    public string DependentLocality { get; set; }
    public string Town { get; set; }
    public string County { get; set; }
    public string Country { get; set; }
    public string Postcode { get; set; }
    public bool ValidatedWithCompaniesHouse { get; set; }
    public bool IsComplianceScheme { get; set; }
    public int OrganisationId { get; set; }
    public bool IsDeleted { get; set; }
    public int? ProducerTypeId { get; set; }
}