using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class UpdatedNpwdPrnsResponseModel
{
    public required string EvidenceNo { get; set; }
    public required string EvidenceStatusCode { get; set; }
    public required string ObligationYear { get; set; }
    public DateTime? StatusDate { get; set; }
}
