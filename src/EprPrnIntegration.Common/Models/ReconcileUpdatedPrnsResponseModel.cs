using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models;

public class ReconcileUpdatedPrnsResponseModel
{
    [JsonPropertyName("PrnNumber")]
    public string PrnNumber { get; set; } = null!;

    [JsonPropertyName("StatusName")]
    public string PrnStatus { get; set; } = null!;

    [JsonPropertyName("UpdatedOn")]
    public string UpdatedOn { get; set; } = null!;

    [JsonPropertyName("OrganisationName")]
    public string OrganisationName { get; set; } = null!;
}
