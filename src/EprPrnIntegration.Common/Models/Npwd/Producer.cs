namespace EprPrnIntegration.Common.Models.Npwd;

using System.Text.Json.Serialization;

public class Producer
{
    [JsonPropertyName("AddressLine1")]
    public string AddressLine1 { get; set; }

    [JsonPropertyName("AddressLine2")]
    public string AddressLine2 { get; set; }

    [JsonPropertyName("CompanyRegNo")]
    public string CompanyRegNo { get; set; }

    [JsonPropertyName("EntityTypeCode")]
    public string EntityTypeCode { get; set; }

    [JsonPropertyName("EntityTypeName")]
    public string EntityTypeName { get; set; }

    [JsonPropertyName("EPRId")]
    public string EPRId { get; set; }

    [JsonPropertyName("EPRCode")]
    public string EPRCode { get; set; }

    [JsonPropertyName("Postcode")]
    public string Postcode { get; set; }

    [JsonPropertyName("ProducerName")]
    public string ProducerName { get; set; }

    [JsonPropertyName("StatusCode")]
    public string StatusCode { get; set; }

    [JsonPropertyName("StatusDesc")]
    public string StatusDesc { get; set; }

    [JsonPropertyName("StatusDate")]
    public string StatusDate { get; set; }
}