using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Mappers;

public static class PackagingRecyclingNoteToSavePrnDetailsRequestMapper
{
    public static SavePrnDetailsRequest Map(PackagingRecyclingNote prn, IConfiguration config, ILogger logger)
    {
        var resolvedYear = ObligationYearResolver.GetDefaultObligationYear(config, logger);

        return new SavePrnDetailsRequest
        {
            AccreditationNo = prn.Accreditation.AccreditationNumber,
            AccreditationYear = prn.Accreditation.AccreditationYear.ToString(),
            CancelledDate = prn.Status.CancelledAt,
            DecemberWaste = prn.IsDecemberWaste,
            EvidenceMaterial = prn.Accreditation.Material,
            EvidenceNo = prn.PrnNumber,
            EvidenceStatusCode = RrepwStatusToPrnStatusMapper.Map(prn.Status.CurrentStatus),
            EvidenceTonnes = prn.TonnageValue,
            IssueDate = prn.Status.AuthorisedAt,
            IssuedByNPWDCode = ParseGuid(prn.IssuedByOrganisation.Id),
            IssuedByOrgName = prn.IssuedByOrganisation.Name,
            IssuedToNPWDCode = ParseGuid(prn.IssuedToOrganisation.Id),
            IssuedToOrgName = prn.IssuedToOrganisation.Name,
            IssuerNotes = prn.IssuerNotes,
            StatusDate = prn.Status.AuthorisedAt ?? prn.Status.AcceptedAt ?? prn.Status.RejectedAt ?? prn.Status.CancelledAt,
            CreatedByUser = "IntegrationFA",
            SourceSystemId = "RREPW",
            // Not available in PackagingRecyclingNote
            IssuedToEPRId = null,
            IssuerRef = "",
            MaterialOperationCode = null,
            ModifiedOn = null,
            ObligationYear = resolvedYear,
            PrnSignatory = null,
            PrnSignatoryPosition = null,
            ProducerAgency = null,
            RecoveryProcessCode = null,
            ReprocessorAgency = null
        };
    }

    private static Guid? ParseGuid(string? input)
    {
        if (Guid.TryParse(input, out var guid))
        {
            return guid;
        }
        return null;
    }

    public static bool IsExport(string evidenceNo)
    {
        if (string.IsNullOrEmpty(evidenceNo))
            return false;

        var val = evidenceNo.Substring(0, 2).Trim();

        return string.Equals(val, ExporterCodePrefixes.EaExport, StringComparison.InvariantCultureIgnoreCase)
               || string.Equals(val, ExporterCodePrefixes.SepaExport, StringComparison.InvariantCultureIgnoreCase);
    }
}
