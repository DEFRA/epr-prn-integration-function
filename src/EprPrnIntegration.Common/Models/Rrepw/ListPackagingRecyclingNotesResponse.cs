using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class ListPackagingRecyclingNotesResponse
{
    [JsonPropertyName("items")]
    public required List<PackagingRecyclingNote> Items { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasMore")]
    public required bool HasMore { get; set; }
}
