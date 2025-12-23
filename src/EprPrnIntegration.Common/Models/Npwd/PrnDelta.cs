using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Models.Npwd;

public class PrnDelta
{
    [JsonProperty("@context")]
    public string Context { get; set; } = default!;

    [JsonProperty("value")]
    public List<UpdatedNpwdPrnsResponseModel> Value { get; set; } = default!;
}
