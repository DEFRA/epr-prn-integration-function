using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class UpdatedNpwdPrnsResponseModel
{
    public required string EvidenceNo { get; set; }
    public required string EvidenceStatusCode { get; set; }
    /// <summary>
    /// http://epr-prn-common-backend/api/v1/prn/ModifiedPrnsByDate endpoint does not return this field yet
    /// </summary>
    public string? ObligationYear { get; set; }
    public DateTime? StatusDate { get; set; }
}
