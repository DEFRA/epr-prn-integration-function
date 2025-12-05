using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class UpdatedProducersResponseV2
{
    public string? OrganisationName { get; set; }
    public string? TradingName { get; set; }
    public string? OrganisationType { get; set; }
    public string? CompaniesHouseNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public string? Country { get; set; }
    public string? Postcode { get; set; }
    public string? PEPRID { get; set; }
    public string? Status { get; set; }
    public string? BusinessCountry { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public required string RegistrationYear { get; set; }
}