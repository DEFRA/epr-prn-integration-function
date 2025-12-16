using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class PackagingRecyclingNote
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("prnNumber")]
    public string? PrnNumber { get; set; }

    [JsonPropertyName("status")]
    public required Status Status { get; set; }

    [JsonPropertyName("issuedByOrganisation")]
    public required Organisation IssuedByOrganisation { get; set; }

    [JsonPropertyName("issuedToOrganisation")]
    public required Organisation IssuedToOrganisation { get; set; }

    [JsonPropertyName("accreditation")]
    public required Accreditation Accreditation { get; set; }

    [JsonPropertyName("isDecemberWaste")]
    public required bool IsDecemberWaste { get; set; }

    [JsonPropertyName("isExport")]
    public required bool IsExport { get; set; }

    [JsonPropertyName("tonnageValue")]
    public required int TonnageValue { get; set; }

    [JsonPropertyName("issuerNotes")]
    public string? IssuerNotes { get; set; }
}
