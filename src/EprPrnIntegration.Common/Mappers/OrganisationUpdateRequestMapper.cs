using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationApi;

namespace EprPrnIntegration.Common.Mappers;

public static class OrganisationUpdateRequestMapper
{
    public static OrganisationUpdateRequest Map(UpdatedProducersResponseV2 updatedProducer)
    {
        if (updatedProducer.PEPRID == null)
        {
            throw new ArgumentException("PEPRID is null");
        }
        
        if (updatedProducer.OrganisationName == null)
        {
            throw new ArgumentException("OrganisationName is null");
        }
        
        return new OrganisationUpdateRequest
        {
            Id = updatedProducer.PEPRID,
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

    private static BusinessCountry? MapBusinessCountry(string? businessCountry)
    {
        return (businessCountry) switch
        {
            "England" => BusinessCountry.England,
            "Northern Ireland" => BusinessCountry.NorthernIreland,
            "Scotland" => BusinessCountry.Scotland,
            "Wales" => BusinessCountry.Wales,
            _ => null
        };
    }

    private static Registration MapRegistration(UpdatedProducersResponseV2 updatedProducer)
    {
        var type = (updatedProducer.OrganisationType) switch
        {
            "DP" => RegistrationType.LargeProducer,
            "S" => RegistrationType.ComplianceScheme,
            _ => throw new ArgumentException($"Unknown registration type {updatedProducer.OrganisationType}")
        };

        var status = (updatedProducer.Status) switch
        {
            "registered" => RegistrationStatus.Registered,
            "deleted" => RegistrationStatus.Cancelled,
            _ => throw new ArgumentException($"Unknown status {updatedProducer.Status}")
        };

        if (updatedProducer.SubmissionYear == null)
        {
            throw new ArgumentException("SubmissionYear is null");
        }

        return new Registration
        {
            Status = status,
            Type = type,
            SubmissionYear = updatedProducer.SubmissionYear.Value
        };
    }
}