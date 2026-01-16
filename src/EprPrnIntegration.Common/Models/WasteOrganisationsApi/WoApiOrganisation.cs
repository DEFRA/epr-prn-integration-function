using System.Text.Json.Serialization;

namespace EprPrnIntegration.Common.Models.WasteOrganisationsApi
{
    public class WoApiOrganisation
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("tradingName")]
        public string? TradingName { get; set; }

        [JsonPropertyName("businessCountry")]
        public string? BusinessCountry { get; set; }

        [JsonPropertyName("companiesHouseNumber")]
        public string? CompaniesHouseNumber { get; set; }

        [JsonPropertyName("address")]
        public required WoApiAddress Address { get; set; }

        [JsonPropertyName("registration")]
        public required List<WoApiRegistration> Registrations { get; set; }
    }
}
