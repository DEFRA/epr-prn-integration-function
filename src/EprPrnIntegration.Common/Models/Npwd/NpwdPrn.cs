using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class NpwdPrn
    {
        public string? AccreditationNo { get; set; }
        public int AccreditationYear { get; set; }
        public DateTime? CancelledDate { get; set; }
        public bool DecemberWaste { get; set; }
        public string? EvidenceMaterial { get; set; }
        public string? EvidenceNo { get; set; }
        public string? EvidenceStatusCode { get; set; }
        public string? EvidenceStatusDesc { get; set; }
        public int EvidenceTonnes { get; set; }
        public DateTime? IssueDate { get; set; }
        public string? IssuedByNPWDCode { get; set; }
        public string? IssuedByOrgName { get; set; }
        public string? IssuedToEPRCode { get; set; }
        public string? IssuedToEPRId { get; set; }
        public string? IssuedToNPWDCode { get; set; }
        public string? IssuedToOrgName { get; set; }
        public string? IssuerNotes { get; set; }
        public string? IssuerRef { get; set; }
        public string? MaterialOperationCode { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ObligationYear { get; set; }
        public string? PRNSignatory { get; set; }
        public string? PRNSignatoryPosition { get; set; }
        public string? ProducerAgency { get; set; }
        public string? RecoveryProcessCode { get; set; }
        public string? ReprocessorAgency { get; set; }
        public DateTime? StatusDate { get; set; }
        public string? CreatedByUser { get; set; }

        public string? IssuedToEntityTypeCode { get; set; }

        public bool IsComplianceScheme => (IssuedToEntityTypeCode ?? string.Empty).Equals("CS");
    }
}
