using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class UpdatedProducersResponseModel
{
    public string ProducerName { get; set; } = default!;
    public string CompaniesHouseNumber { get; set; } = default!;
    public string TradingName { get; set; } = default!;
    public string ReferenceNumber { get; set; } = default!;
    public string SubBuildingName { get; set; } = default!;
    public string BuildingName { get; set; } = default!;
    public string BuildingNumber { get; set; } = default!;
    public string Street { get; set; } = default!;
    public string Locality { get; set; } = default!;
    public string DependentLocality { get; set; } = default!;
    public string Town { get; set; } = default!;
    public string County { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string Postcode { get; set; } = default!;
    public bool ValidatedWithCompaniesHouse { get; set; }
    public bool IsComplianceScheme { get; set; }
    public int OrganisationId { get; set; }
    public bool IsDeleted { get; set; }
    public int? ProducerTypeId { get; set; }
    public string ExternalId { get; set; } = default!;

    public string OrganisationAddress => string.Join(", ", new[] {
                SubBuildingName,
                BuildingNumber,
                BuildingName,
                Street,
                Town,
                County,
                Postcode,
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
}