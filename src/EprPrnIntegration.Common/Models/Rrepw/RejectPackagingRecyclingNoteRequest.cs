using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class RejectPackagingRecyclingNoteRequest
{
    [JsonPropertyName("rejectedAt")]
    public DateTime? RejectedAt { get; set; }
}
