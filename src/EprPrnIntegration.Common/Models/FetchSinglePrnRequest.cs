using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models;

public class FetchSinglePrnRequest
{
    [JsonPropertyName("prnNumber")]
    public string PrnNumber { get; set; }
}