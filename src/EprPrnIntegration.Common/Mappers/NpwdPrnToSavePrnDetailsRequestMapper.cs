using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Mappers;

public static class NpwdPrnToSavePrnDetailsRequestMapper
{
    public static SaveNpwdPrnDetailsRequest Map(
        NpwdPrn npwdPrn,
        IConfiguration config,
        ILogger logger
    )
    {
        var resolvedYear = ObligationYearResolver.GetDefaultObligationYear(config, logger);

        return new SaveNpwdPrnDetailsRequest
        {
            AccreditationNo = npwdPrn.AccreditationNo,
            AccreditationYear = npwdPrn.AccreditationYear.ToString(),
            CancelledDate = npwdPrn.CancelledDate,
            DecemberWaste = npwdPrn.DecemberWaste,
            EvidenceMaterial = npwdPrn.EvidenceMaterial,
            EvidenceNo = npwdPrn.EvidenceNo,
            EvidenceStatusCode = NpwdStatusToPrnStatusMapper.Map(npwdPrn.EvidenceStatusCode!),
            EvidenceTonnes = npwdPrn.EvidenceTonnes,
            IssueDate = npwdPrn.IssueDate,
            IssuedByNPWDCode = ParseGuid(npwdPrn.IssuedByNPWDCode),
            IssuedByOrgName = npwdPrn.IssuedByOrgName,
            IssuedToNPWDCode = ParseGuid(npwdPrn.IssuedToNPWDCode),
            IssuedToOrgName = npwdPrn.IssuedToOrgName,
            IssuedToEPRId = ParseGuid(npwdPrn.IssuedToEPRId),
            IssuerNotes = npwdPrn.IssuerNotes,
            IssuerRef = npwdPrn.IssuerRef ?? "", // Null is converted to empty for now this need discussion Data Arch
            MaterialOperationCode = ParseGuid(npwdPrn.MaterialOperationCode),
            ModifiedOn = npwdPrn.ModifiedOn,
            ObligationYear = npwdPrn.ObligationYear?.ToString() ?? resolvedYear,
            PrnSignatory = npwdPrn.PRNSignatory,
            PrnSignatoryPosition = npwdPrn.PRNSignatoryPosition,
            ProducerAgency = npwdPrn.ProducerAgency,
            RecoveryProcessCode = npwdPrn.RecoveryProcessCode,
            ReprocessorAgency = npwdPrn.ReprocessorAgency,
            StatusDate = npwdPrn.StatusDate,
            CreatedByUser = "IntegrationFA",
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

        return string.Equals(
                val,
                ExporterCodePrefixes.EaExport,
                StringComparison.InvariantCultureIgnoreCase
            )
            || string.Equals(
                val,
                ExporterCodePrefixes.SepaExport,
                StringComparison.InvariantCultureIgnoreCase
            );
    }
}
