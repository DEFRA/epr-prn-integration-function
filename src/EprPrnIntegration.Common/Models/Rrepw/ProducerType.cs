using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EprPrnIntegration.Common.Models.Rrepw;

[JsonConverter(typeof(StringEnumConverter))]
public enum ProducerType
{
    CS, // Compliance Scheme
    DR, // Direct Registrant
}