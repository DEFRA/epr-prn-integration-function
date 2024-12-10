using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.Mappers
{

    public static class NpwdPrnToSavePrnDetailsRequestMapper
    {
        public static SavePrnDetailsRequest Map(NpwdPrn npwdPrn)
        {
            return new SavePrnDetailsRequest
            {
                ExternalId = Guid.NewGuid(),
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
                IssuerRef = npwdPrn.IssuerRef,
                MaterialOperationCode = ParseGuid(npwdPrn.MaterialOperationCode),
                ModifiedOn = npwdPrn.ModifiedOn,
                ObligationYear = npwdPrn.ObligationYear?.ToString() ?? DateTime.MinValue.ToString(),
                PrnSignatory = npwdPrn.PRNSignatory,
                PrnSignatoryPosition = npwdPrn.PRNSignatoryPosition,
                ProducerAgency = npwdPrn.ProducerAgency,
                RecoveryProcessCode = npwdPrn.RecoveryProcessCode,
                ReprocessorAgency = npwdPrn.ReprocessorAgency,
                StatusDate = npwdPrn.StatusDate,
                CreatedByUser = "IntegrationFA"
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
    }
}
