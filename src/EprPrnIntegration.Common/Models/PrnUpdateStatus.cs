using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class PrnUpdateStatus
{
    [JsonPropertyName("prnNumber")]
    public required string PrnNumber { get; set; }

    [JsonPropertyName("prnStatusId")]
    public required int PrnStatusId { get; set; }

    [JsonPropertyName("statusDate")]
    public DateTime? StatusDate { get; set; }

    [JsonPropertyName("accreditationYear")]
    public required string AccreditationYear { get; set; }

    [JsonPropertyName("sourceSystemId")]
    public required string SourceSystemId { get; set; }
}
