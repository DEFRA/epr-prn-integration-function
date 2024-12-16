namespace EprPrnIntegration.Common.Models.Npwd
{
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class Evidence
    {
        [JsonProperty("AccreditationNo")]
        public string? AccreditationNo { get; set; }

        [JsonProperty("AccreditationYear")]
        public int AccreditationYear { get; set; }

        [JsonProperty("CancelledDate")]
        public DateTime CancelledDate { get; set; }

        [JsonProperty("DecemberWaste")]
        public bool DecemberWaste { get; set; }

        [JsonProperty("EvidenceMaterial")]
        public string? EvidenceMaterial { get; set; }

        [JsonProperty("EvidenceNo")]
        public string? EvidenceNo { get; set; }

        [JsonProperty("EvidenceStatusCode")]
        public string? EvidenceStatusCode { get; set; }

        [JsonProperty("EvidenceStatusDesc")]
        public string? EvidenceStatusDesc { get; set; }

        [JsonProperty("EvidenceTonnes")]
        public double EvidenceTonnes { get; set; }

        [JsonProperty("IssueDate")]
        public DateTime IssueDate { get; set; }

        [JsonProperty("IssuedByNPWDCode")]
        public string? IssuedByNPWDCode { get; set; }

        [JsonProperty("IssuedByOrgName")]
        public string? IssuedByOrgName { get; set; }

        [JsonProperty("IssuedToEPRCode")]
        public string? IssuedToEPRCode { get; set; }

        [JsonProperty("IssuedToEPRId")]
        public string? IssuedToEPRId { get; set; }

        [JsonProperty("IssuedToNPWDCode")]
        public string? IssuedToNPWDCode { get; set; }

        [JsonProperty("IssuedToOrgName")]
        public string? IssuedToOrgName { get; set; }

        [JsonProperty("IssuerNotes")]
        public string? IssuerNotes { get; set; }

        [JsonProperty("MaterialOperationCode")]
        public string? MaterialOperationCode { get; set; }

        [JsonProperty("ModifiedOn")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("PRNSignatory")]
        public string? PRNSignatory { get; set; }

        [JsonProperty("ProducerAgency")]
        public string? ProducerAgency { get; set; }

        [JsonProperty("RecoveryProcessCode")]
        public string? RecoveryProcessCode { get; set; }

        [JsonProperty("ReprocessorAgency")]
        public string? ReprocessorAgency { get; set; }

        [JsonProperty("StatusDate")]
        public DateTime StatusDate { get; set; }
    }

}
