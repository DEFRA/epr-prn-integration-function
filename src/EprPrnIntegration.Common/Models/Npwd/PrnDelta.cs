using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Models.Npwd;

public class PrnDelta
{
    [JsonProperty("@context")]
    public string Context { get; set; }

    [JsonProperty("value")]
    public List<UpdatedPrnsResponseModel> Value { get; set; }
}