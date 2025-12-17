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
    public static SavePrnDetailsRequestV2 Map(PackagingRecyclingNote prn)
    {
        return new SavePrnDetailsRequestV2
        {
            PrnNumber = prn.PrnNumber!,
            SourceSystemId = "RREPW",
            PrnStatusId = 1,
            PrnSignatory = "DUMMY_SIGNATORY",
            PrnSignatoryPosition = null,
            StatusUpdatedOn = DateTime.UtcNow,
            IssuedByOrg = "DUMMY_ISSUED_BY_ORG",
            OrganisationId = Guid.Empty,
            OrganisationName = "DUMMY_ORG_NAME",
            AccreditationNumber = "DUMMY_ACCREDITATION",
            AccreditationYear = "2024",
            MaterialName = "DUMMY_MATERIAL",
            ReprocessorExporterAgency = "DUMMY_AGENCY",
            ReprocessingSite = null,
            DecemberWaste = false,
            IsExport = false,
            TonnageValue = 0,
            IssuerNotes = null,
            ProcessToBeUsed = "DUMMY_PROCESS",
            ObligationYear = "2024"
        };
    }
}
