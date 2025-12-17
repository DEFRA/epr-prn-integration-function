using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Enums;
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
        return new SavePrnDetailsRequest
        {
            AccreditationNo = "DUMMY_ACCREDITATION",
            AccreditationYear = "2024",
            CancelledDate = null,
            DecemberWaste = false,
            EvidenceMaterial = "DUMMY_MATERIAL",
            EvidenceNo = "DUMMY_EVIDENCE_NO",
            EvidenceStatusCode = EprnStatus.ACCEPTED,
            EvidenceTonnes = 0,
            IssueDate = DateTime.UtcNow,
            IssuedByNPWDCode = Guid.Empty,
            IssuedByOrgName = "DUMMY_ISSUED_BY_ORG",
            IssuedToNPWDCode = Guid.Empty,
            IssuedToOrgName = "DUMMY_ISSUED_TO_ORG",
            IssuerNotes = "DUMMY_NOTES",
            StatusDate = DateTime.UtcNow,
            CreatedByUser = "IntegrationFA",
            SourceSystemId = "RREPW",
            IssuedToEPRId = null,
            IssuerRef = "DUMMY_REF",
            MaterialOperationCode = null,
            ModifiedOn = null,
            ObligationYear = "2024",
            PrnSignatory = null,
            PrnSignatoryPosition = null,
            ProducerAgency = null,
            RecoveryProcessCode = null,
            ReprocessorAgency = null
        };
    }
}
