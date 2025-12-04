using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Common.Mappers;

public static class WasteOrganisationsApiUpdateRequestMapper
{
    public static WasteOrganisationsApiUpdateRequest Map(UpdatedProducersResponseV2 updatedProducer)
    {
        if (updatedProducer.PEPRID == null)
        {
            throw new ArgumentException("PEPRID is null");
        }
        
        if (updatedProducer.OrganisationName == null)
        {
            throw new ArgumentException("OrganisationName is null");
        }
        
        return new WasteOrganisationsApiUpdateRequest
        {
            Name = updatedProducer.OrganisationName,
            TradingName = updatedProducer.TradingName,
            CompaniesHouseNumber = updatedProducer.CompaniesHouseNumber,
            BusinessCountry = MapBusinessCountry(updatedProducer.BusinessCountry),
            Registration = MapRegistration(updatedProducer),
            Address = MapAddress(updatedProducer),
        };
    }

    private static Address MapAddress(UpdatedProducersResponseV2 updatedProducer)
    {
        return new Address
        {
            AddressLine1 = updatedProducer.AddressLine1,
            AddressLine2 = updatedProducer.AddressLine2,
            Town = updatedProducer.Town,
            County = updatedProducer.County,
            Postcode = updatedProducer.Postcode,
            Country = updatedProducer.Country
        };
    }

    private static string? MapBusinessCountry(string? businessCountry)
    {
        return (businessCountry) switch
        {
            "England" => "GB-ENG",
            "Northern Ireland" => "GB-NIR",
            "Scotland" => "GB-SCT",
            "Wales" => "GB-WLS",
            _ => null
        };
    }

    private static Registration MapRegistration(UpdatedProducersResponseV2 updatedProducer)
    {
        var type = (updatedProducer.OrganisationType) switch
        {
            "DP" => "LARGE_PRODUCER",
            "CS" => "COMPLIANCE_SCHEME",
            _ => throw new ArgumentException($"Unknown registration type {updatedProducer.OrganisationType}")
        };

        var status = updatedProducer.Status?.ToLowerInvariant() switch
        {
            "registered" => "REGISTERED",
            "deleted" => "CANCELLED",
            _ => throw new ArgumentException($"Unknown status {updatedProducer.Status}")
        };

        if (!int.TryParse(updatedProducer.RegistrationYear, out var registrationYear))
        {
            throw new ArgumentException($"RegistrationYear '{updatedProducer.RegistrationYear}' is not a valid integer");
        }

        return new Registration
        {
            Status = status,
            Type = type,
            RegistrationYear = registrationYear
        };
    }
}