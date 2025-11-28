using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EprPrnIntegration.Common.Models.WasteOrganisationsApi;

public class WasteOrganisationsApiUpdateRequest
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }

    [JsonProperty("tradingName")]
    public string? TradingName { get; set; }

    [JsonProperty("businessCountry")]
    public BusinessCountry? BusinessCountry { get; set; }

    [JsonProperty("companiesHouseNumber")]
    public string? CompaniesHouseNumber { get; set; }

    [JsonProperty("address")]
    public required Address Address { get; set; }

    [JsonProperty("registration")]
    public required Registration Registration { get; set; }
}

public class Address
{
    [JsonProperty("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonProperty("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonProperty("postcode")]
    public string? Postcode { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("county")]
    public string? County { get; set; }

    [JsonProperty("town")]
    public string? Town { get; set; }
}

public class Registration
{
    [JsonProperty("status")]
    public required RegistrationStatus Status { get; set; }

    [JsonProperty("type")]
    public required RegistrationType Type { get; set; }

    [JsonProperty("submissionYear")]
    public required int SubmissionYear { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum RegistrationStatus
{
    [EnumMember(Value = "REGISTERED")]
    Registered,

    [EnumMember(Value = "CANCELLED")]
    Cancelled
}

[JsonConverter(typeof(StringEnumConverter))]
public enum RegistrationType
{
    [EnumMember(Value = "SMALL-PRODUCER")]
    SmallProducer,

    [EnumMember(Value = "LARGE-PRODUCER")]
    LargeProducer,

    [EnumMember(Value = "COMPLIANCE-SCHEME")]
    ComplianceScheme,

    [EnumMember(Value = "REPROCESSOR")]
    Reprocessor,

    [EnumMember(Value = "EXPORTER")]
    Exporter
}

[JsonConverter(typeof(StringEnumConverter))]
public enum BusinessCountry
{
    [EnumMember(Value = "GB-ENG")]
    England,

    [EnumMember(Value = "GB-NIR")]
    NorthernIreland,

    [EnumMember(Value = "GB-SCT")]
    Scotland,

    [EnumMember(Value = "GB-WLS")]
    Wales
}