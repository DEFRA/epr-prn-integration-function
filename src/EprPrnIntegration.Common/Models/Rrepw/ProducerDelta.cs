using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EprPrnIntegration.Common.Models.Rrepw;

public class ProducerUpdateRequest
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

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

[JsonConverter(typeof(StringEnumConverter))]
public enum ProducerType
{
    CS, // Compliance Scheme
    DR, // Direct Registrant
    CSM // DR Moved to CS - no longer valid for PRN Issuance
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ProducerStatus
{
    [EnumMember(Value = "PR-REGISTERED")]
    PrRegistered,
    [EnumMember(Value = "PR-CANCELLED")]
    PrCancelled,
    [EnumMember(Value = "CSR-REGISTERED")]
    CsrRegistered,
    [EnumMember(Value = "CSR-CANCELLED")]
    CsrCancelled
}