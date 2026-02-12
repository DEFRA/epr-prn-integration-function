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
        var registration = source.Organisation?.Registrations.FirstOrDefault(x =>
            x.Status == WoApiOrganisationStatus.Registered &&
            x.RegistrationYear == source.Accreditation?.AccreditationYear);

        if (registration is not null && registration.Type == WoApiOrganisationType.LargeProducer)
            return source.IssuedToOrganisation?.Name;

        if (registration is not null && registration.Type == WoApiOrganisationType.ComplianceScheme)
            return source.IssuedToOrganisation?.TradingName;
        
        logger.LogWarning("Fallback trading name mapping for organisation {Id}", source.Organisation?.Id);
        
        return string.IsNullOrWhiteSpace(source.IssuedToOrganisation?.TradingName)
            ? source.IssuedToOrganisation?.Name
            : source.IssuedToOrganisation?.TradingName;
    }
}