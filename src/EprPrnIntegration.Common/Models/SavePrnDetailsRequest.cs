using EprPrnIntegration.Common.Enums;
using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class SavePrnDetailsRequest
{
    public string? AccreditationNo { get; set; }
    public string? AccreditationYear { get; set; }
    public DateTime? CancelledDate { get; set; }
    public bool? DecemberWaste { get; set; }
    public string? EvidenceMaterial { get; set; }
    public string? EvidenceNo { get; set; }
    public PrnStatus? EvidenceStatusCode { get; set; }
    public int? EvidenceTonnes { get; set; }
    public DateTime? IssueDate { get; set; }
    public Guid? IssuedByNPWDCode { get; set; }
    public string? IssuedByOrgName { get; set; }
    public Guid? IssuedToNPWDCode { get; set; }
    public string? IssuedToOrgName { get; set; }
    public Guid? IssuedToEPRId { get; set; }
    public string? IssuerNotes { get; set; }
    public string? IssuerRef { get; set; }
    public Guid? MaterialOperationCode { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string? ObligationYear { get; set; }
    public string? PrnSignatory { get; set; }
    public string? PrnSignatoryPosition { get; set; }
    public string? ProducerAgency { get; set; }
    public string? RecoveryProcessCode { get; set; }
    public string? ReprocessorAgency { get; set; }
    public DateTime? StatusDate { get; set; }
    public Guid? ExternalId { get; set; }
}