using AutoMapper;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Mappers;

public class OrganisationNameResolver(ILogger<OrganisationNameResolver> logger) : IValueResolver<PackagingRecyclingNote, SavePrnDetailsRequest, string?>
{
    public string? Resolve(
        PackagingRecyclingNote source,
        SavePrnDetailsRequest destination,
        string? destMember,
        ResolutionContext context)
    {
        var registrations = source.Organisation?.Registrations
            .Where(x =>
                x.Status == WoApiOrganisationStatus.Registered &&
                x.RegistrationYear == source.Accreditation?.AccreditationYear)
            .ToList() ?? [];

        var registration = registrations.FirstOrDefault(x => x.Type == WoApiOrganisationType.ComplianceScheme);
        if (registration is not null)
            return string.IsNullOrWhiteSpace(source.IssuedToOrganisation?.TradingName)
                ? source.IssuedToOrganisation?.Name
                : source.IssuedToOrganisation?.TradingName;

        registration = registrations.FirstOrDefault(x => x.Type == WoApiOrganisationType.LargeProducer);
        if (registration is not null)
            return source.IssuedToOrganisation?.Name;
        
        logger.LogWarning("Fallback trading name or name mapping for organisation {Id}", source.Organisation?.Id);
        
        return string.IsNullOrWhiteSpace(source.IssuedToOrganisation?.TradingName)
            ? source.IssuedToOrganisation?.Name
            : source.IssuedToOrganisation?.TradingName;
    }
}