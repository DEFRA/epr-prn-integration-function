using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class PrnUpdateStatus
{
    public required string PrnNumber { get; set; }
    public required int PrnStatusId { get; set; }
    public DateTime? StatusDate { get; set; }
    public required string AccreditationYear { get; set; }
    public required string SourceSystemId { get; set; }
}
