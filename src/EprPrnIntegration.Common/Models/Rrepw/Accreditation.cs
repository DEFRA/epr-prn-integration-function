using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class Accreditation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("accreditationNumber")]
    public string? AccreditationNumber { get; set; }

    [JsonPropertyName("accreditationYear")]
    public int? AccreditationYear { get; set; }

    [JsonPropertyName("material")]
    public string? Material { get; set; }

    [JsonPropertyName("submittedToRegulator")]
    public string? SubmittedToRegulator { get; set; }

    [JsonPropertyName("glassRecyclingProcess")]
    public string? GlassRecyclingProcess { get; set; }

    [JsonPropertyName("siteAddress")]
    public Address? SiteAddress { get; set; }
}
