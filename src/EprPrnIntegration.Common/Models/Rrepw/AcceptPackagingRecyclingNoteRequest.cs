using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class AcceptPackagingRecyclingNoteRequest
{
    [JsonPropertyName("acceptedAt")]
    public DateTime? AcceptedAt { get; set; }
}
