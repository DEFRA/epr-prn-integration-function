using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class PackagingRecyclingNote
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("prnNumber")]
    public string? PrnNumber { get; set; }

    [JsonPropertyName("status")]
    public Status? Status { get; set; }

    [JsonPropertyName("issuedByOrganisation")]
    public Organisation? IssuedByOrganisation { get; set; }

    [JsonPropertyName("issuedToOrganisation")]
    public Organisation? IssuedToOrganisation { get; set; }

    [JsonPropertyName("accreditation")]
    public Accreditation? Accreditation { get; set; }

    [JsonPropertyName("isDecemberWaste")]
    public bool? IsDecemberWaste { get; set; }

    [JsonPropertyName("isExport")]
    public bool? IsExport { get; set; }

    [JsonPropertyName("tonnageValue")]
    public int? TonnageValue { get; set; }

    [JsonPropertyName("issuerNotes")]
    public string? IssuerNotes { get; set; }

    // Used for mapping only
    public WoApiOrganisation? Organisation { get; set; }
}
