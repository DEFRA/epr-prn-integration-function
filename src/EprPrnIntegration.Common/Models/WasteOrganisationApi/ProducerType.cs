using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EprPrnIntegration.Common.Models.WasteOrganisationApi;

[JsonConverter(typeof(StringEnumConverter))]
public enum ProducerType
{
    CS, // Compliance Scheme
    DR, // Direct Registrant
}