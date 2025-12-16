using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class Accreditation
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("accreditationNumber")]
    public required string AccreditationNumber { get; set; }

    [JsonPropertyName("accreditationYear")]
    public required int AccreditationYear { get; set; }

    [JsonPropertyName("material")]
    public required string Material { get; set; }

    [JsonPropertyName("submittedToRegulator")]
    public required string SubmittedToRegulator { get; set; }

    [JsonPropertyName("glassRecyclingProcess")]
    public string? GlassRecyclingProcess { get; set; }

    [JsonPropertyName("siteAddress")]
    public Address? SiteAddress { get; set; }
}
