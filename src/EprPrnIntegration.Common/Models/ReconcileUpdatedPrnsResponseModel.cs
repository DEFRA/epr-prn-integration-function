using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Models;

public class ReconcileUpdatedPrnsResponseModel
{
    [JsonProperty("PrnNumber")]
    public string PrnNumber { get; set; } = null!;

    [JsonProperty("StatusName")]
    public string StatusName { get; set; } = null!;

    [JsonProperty("UpdatedOn")]
    public string UpdatedOn { get; set; } = null!;

    [JsonProperty("OrganisationName")]
    public string OrganisationName { get; set; } = null!;
}
