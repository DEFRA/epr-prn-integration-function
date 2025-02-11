namespace EprPrnIntegration.Common.Models.Npwd;

using System.Text.Json.Serialization;

public class Producer
{
    [JsonPropertyName("AddressLine1")]
    public string AddressLine1 { get; set; } = default!;

    [JsonPropertyName("AddressLine2")]
    public string AddressLine2 { get; set; } = default!;

    [JsonPropertyName("AddressLine3")]
    public string AddressLine3 { get; set; } = default!;

    [JsonPropertyName("AddressLine4")]
    public string AddressLine4 { get; set; } = default!;

    [JsonPropertyName("Country")]
    public string Country { get; set; } = default!;

    [JsonPropertyName("County")]
    public string County { get; set; } = default!;

    [JsonPropertyName("Town")]
    public string Town { get; set; } = default!;

    [JsonPropertyName("CompanyRegNo")]
    public string CompanyRegNo { get; set; } = default!;

    [JsonPropertyName("EntityTypeCode")]
    public string EntityTypeCode { get; set; } = default!;

    [JsonPropertyName("EntityTypeName")]
    public string EntityTypeName { get; set; } = default!;

    [JsonPropertyName("EPRId")]
    public string EPRId { get; set; } = default!;

    [JsonPropertyName("EPRCode")]
    public string EPRCode { get; set; } = default!;

    [JsonPropertyName("Postcode")]
    public string Postcode { get; set; } = default!;

    [JsonPropertyName("ProducerName")]
    public string ProducerName { get; set; } = default!;

    [JsonPropertyName("StatusCode")]
    public string StatusCode { get; set; } = default!;
    [JsonPropertyName("StatusDesc")]
    public string StatusDesc { get; set; } = default!;
    [JsonPropertyName("Agency")]
    public string Agency { get; set; } = default!;
}