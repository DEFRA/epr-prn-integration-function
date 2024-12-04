using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class UpdatedPrnsResponseModel
{
    public required string EvidenceNo { get; set; }
    public required string EvidenceStatusCode { get; set; }
}