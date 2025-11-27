using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Models.WasteOrganisationApi;

public class OrganisationUpdateRequest
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }

    [JsonProperty("tradingName")]
    public string? TradingName { get; set; }

    [JsonProperty("type")]
    public ProducerType? Type { get; set; } 

    [JsonProperty("status")]
    public ProducerStatus? Status { get; set; }

    [JsonProperty("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonProperty("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonProperty("town")]
    public string? Town { get; set; }

    [JsonProperty("county")]
    public string? County { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("postcode")]
    public string? Postcode { get; set; }
}