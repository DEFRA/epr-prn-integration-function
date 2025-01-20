using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Models.Npwd;

public class ProducerDelta
{
    [JsonProperty("@context")]
    public string Context { get; set; } = default!;

    [JsonProperty("value")]
    public List<Producer> Value { get; set; } = default!;
}