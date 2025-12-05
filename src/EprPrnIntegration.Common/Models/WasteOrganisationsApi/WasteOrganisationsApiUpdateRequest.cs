using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.WasteOrganisationsApi;

public class WasteOrganisationsApiUpdateRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("tradingName")]
    public string? TradingName { get; set; }

    [JsonPropertyName("businessCountry")]
    public string? BusinessCountry { get; set; }

    [JsonPropertyName("companiesHouseNumber")]
    public string? CompaniesHouseNumber { get; set; }

    [JsonPropertyName("address")]
    public required Address Address { get; set; }

    [JsonPropertyName("registration")]
    public required Registration Registration { get; set; }
}

public class Address
{
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("town")]
    public string? Town { get; set; }

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public class Registration
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("registrationYear")]
    public required int RegistrationYear { get; set; }
}