using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class UserSummary
{
    [JsonPropertyName("fullName")]
    public required string FullName { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }
}
