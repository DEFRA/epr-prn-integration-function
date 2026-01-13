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

    private static WoApiAddress MapAddress(UpdatedProducersResponseV2 updatedProducer)
    {
        return new WoApiAddress
        {
            AddressLine1 = updatedProducer.AddressLine1,
            AddressLine2 = updatedProducer.AddressLine2,
            Town = updatedProducer.Town,
            County = updatedProducer.County,
            Postcode = updatedProducer.Postcode,
            Country = updatedProducer.Country,
        };
    }

    private static string? MapBusinessCountry(string? businessCountry)
    {
        return businessCountry switch
        {
            BusinessCountry.England => WoApiBusinessCountry.England,
            BusinessCountry.NorthernIreland => WoApiBusinessCountry.NorthernIreland,
            BusinessCountry.Scotland => WoApiBusinessCountry.Scotland,
            BusinessCountry.Wales => WoApiBusinessCountry.Wales,
            _ => null,
        };
    }

    private static WoApiRegistration MapRegistration(UpdatedProducersResponseV2 updatedProducer)
    {
        var type = updatedProducer.OrganisationType switch
        {
            OrganisationType.LargeProducer_DP => WoApiOrganisationType.LargeProducer,
            OrganisationType.ComplianceScheme_CS => WoApiOrganisationType.ComplianceScheme,
            _ => throw new ArgumentException(
                $"Unknown registration type {updatedProducer.OrganisationType}"
            ),
        };

        var status = updatedProducer.Status?.ToLowerInvariant() switch
        {
            OrganisationStatus.Registered => WoApiOrganisationStatus.Registered,
            OrganisationStatus.Deleted => WoApiOrganisationStatus.Cancelled,
            _ => throw new ArgumentException($"Unknown status {updatedProducer.Status}"),
        };

        if (!int.TryParse(updatedProducer.RegistrationYear, out var registrationYear))
        {
            throw new ArgumentException(
                $"RegistrationYear '{updatedProducer.RegistrationYear}' is not a valid integer"
            );
        }

        return new WoApiRegistration
        {
            Status = status,
            Type = type,
            RegistrationYear = registrationYear,
        };
    }
}

public class OrganisationType
{
    public const string ComplianceScheme_CS = "CS";

    /// <summary>
    /// there is a mistake in the Stored procedure for retrieving this which returns this as DP it should be DR.
    /// Alternatively, it should not be using 2 letter acronyms at all and use full words.
    /// </summary>
    public const string LargeProducer_DP = "DP";
    public const string LargeProducer_DR = "DR";
}

public class OrganisationStatus
{
    public const string Registered = "registered";
    public const string Deleted = "deleted";
}

public class BusinessCountry
{
    public const string England = "England";
    public const string NorthernIreland = "Northern Ireland";
    public const string Scotland = "Scotland";
    public const string Wales = "Wales";
}
