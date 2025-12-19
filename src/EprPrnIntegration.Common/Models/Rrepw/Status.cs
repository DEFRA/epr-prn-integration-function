using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.Rrepw;

[ExcludeFromCodeCoverage]
public class Status
{
    [JsonPropertyName("currentStatus")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("authorisedBy")]
    public UserSummary? AuthorisedBy { get; set; }

    [JsonPropertyName("authorisedAt")]
    public DateTime? AuthorisedAt { get; set; }

    [JsonPropertyName("acceptedAt")]
    public DateTime? AcceptedAt { get; set; }

    [JsonPropertyName("rejectedAt")]
    public DateTime? RejectedAt { get; set; }

    [JsonPropertyName("cancelledAt")]
    public DateTime? CancelledAt { get; set; }
}
