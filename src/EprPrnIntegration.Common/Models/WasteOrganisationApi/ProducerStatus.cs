using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EprPrnIntegration.Common.Models.WasteOrganisationApi;

[JsonConverter(typeof(StringEnumConverter))]
public enum ProducerStatus
{
    [EnumMember(Value = "PR-REGISTERED")]
    PrRegistered,
    [EnumMember(Value = "PR-CANCELLED")]
    PrCancelled,
    [EnumMember(Value = "CSR-REGISTERED")]
    CsrRegistered,
    [EnumMember(Value = "CSR-CANCELLED")]
    CsrCancelled
}