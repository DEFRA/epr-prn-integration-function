using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.WasteOrganisationsApi;

public class WasteOrganisationsApiUpdateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tradingName")]
    public string? TradingName { get; set; }

    [JsonPropertyName("businessCountry")]
    public string? BusinessCountry { get; set; }

    [JsonPropertyName("companiesHouseNumber")]
    public string? CompaniesHouseNumber { get; set; }

    [JsonPropertyName("address")]
    public WoApiAddress Address { get; set; } = new();

    [JsonPropertyName("registration")]
    public WoApiRegistration Registration { get; set; } = new();
}

public class WoApiAddress
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

public class WoApiRegistration
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("registrationYear")]
    public int RegistrationYear { get; set; }
}
