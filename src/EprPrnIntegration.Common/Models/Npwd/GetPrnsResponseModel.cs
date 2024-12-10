using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class GetPrnsResponseModel
    {
        [JsonProperty("@odata.context")]
        public string Context { get; set; } = null!;
        public List<NpwdPrn> Value { get; set; } = [];

        [JsonProperty("@odata.nextLink")]
        public string? NextLink { get; set; }
    }
}
