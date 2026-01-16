using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Extensions;

public static class WoApiOrganisationExtensions
{
    /// <summary>
    /// Gets the entity type code for an organisation based on its registrations for a specific year.
    /// </summary>
    /// <param name="organisation">The organisation to check</param>
    /// <param name="year">The registration year to check</param>
    /// <param name="logger">Logger for error messages</param>
    /// <returns>The entity type code (ComplianceScheme_CS or LargeProducer_DR), or null if unable to determine</returns>
    public static string? GetEntityTypeCode(
        this WoApiOrganisation organisation,
        int year,
        ILogger logger
    )
    {
        if (organisation.Registrations == null || !organisation.Registrations.Any())
        {
            logger.LogError(
                "No registrations found for organisation {OrganisationId} in {Year}",
                organisation.Id,
                year
            );
            return null;
        }

        var registeredThisYear = organisation
            .Registrations.Where(r =>
                r.RegistrationYear == year && r.Status == WoApiOrganisationStatus.Registered
            )
            .ToList();

        var isComplianceScheme = registeredThisYear.Any(r =>
            r.Type == WoApiOrganisationType.ComplianceScheme
        );
        var isLargeProducer = registeredThisYear.Any(r =>
            r.Type == WoApiOrganisationType.LargeProducer
        );

        if (isComplianceScheme && isLargeProducer)
        {
            // this should not be possible so log error if it occurs
            logger.LogError(
                "Organisation {OrganisationId} registered as compliance scheme and large producer for the same year in {Year}",
                organisation.Id,
                year
            );
            return null;
        }

        if (!isComplianceScheme && !isLargeProducer)
        {
            logger.LogError(
                "Unknown registration type for organisation {OrganisationId} in {Year}",
                organisation.Id,
                year
            );
            return null;
        }

        return isComplianceScheme
            ? OrganisationType.ComplianceScheme_CS
            : OrganisationType.LargeProducer_DR;
    }
}
