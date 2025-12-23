namespace EprPrnIntegration.Common.Models.Npwd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    [ExcludeFromCodeCoverage]
    public class Evidence
    {
        [JsonProperty(nameof(AccreditationNo))]
        public string? AccreditationNo { get; set; }

        [JsonProperty(nameof(AccreditationYear))]
        public int AccreditationYear { get; set; }

        [JsonProperty(nameof(CancelledDate))]
        public DateTime CancelledDate { get; set; }

        [JsonProperty(nameof(DecemberWaste))]
        public bool DecemberWaste { get; set; }

        [JsonProperty(nameof(EvidenceMaterial))]
        public string? EvidenceMaterial { get; set; }

        [JsonProperty(nameof(EvidenceNo))]
        public string? EvidenceNo { get; set; }

        [JsonProperty(nameof(EvidenceStatusCode))]
        public string? EvidenceStatusCode { get; set; }

        [JsonProperty(nameof(EvidenceStatusDesc))]
        public string? EvidenceStatusDesc { get; set; }

        [JsonProperty(nameof(EvidenceTonnes))]
        public double EvidenceTonnes { get; set; }

        [JsonProperty(nameof(IssueDate))]
        public DateTime IssueDate { get; set; }

        [JsonProperty(nameof(IssuedByNPWDCode))]
        public string? IssuedByNPWDCode { get; set; }

        [JsonProperty(nameof(IssuedByOrgName))]
        public string? IssuedByOrgName { get; set; }

        [JsonProperty(nameof(IssuedToEPRCode))]
        public string? IssuedToEPRCode { get; set; }

        [JsonProperty(nameof(IssuedToEPRId))]
        public string? IssuedToEPRId { get; set; }

        [JsonProperty(nameof(IssuedToNPWDCode))]
        public string? IssuedToNPWDCode { get; set; }

        [JsonProperty(nameof(IssuedToOrgName))]
        public string? IssuedToOrgName { get; set; }

        [JsonProperty(nameof(IssuerNotes))]
        public string? IssuerNotes { get; set; }

        [JsonProperty(nameof(MaterialOperationCode))]
        public string? MaterialOperationCode { get; set; }

        [JsonProperty(nameof(ModifiedOn))]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty(nameof(PRNSignatory))]
        public string? PRNSignatory { get; set; }

        [JsonProperty(nameof(ProducerAgency))]
        public string? ProducerAgency { get; set; }

        [JsonProperty(nameof(RecoveryProcessCode))]
        public string? RecoveryProcessCode { get; set; }

        [JsonProperty(nameof(ReprocessorAgency))]
        public string? ReprocessorAgency { get; set; }

        [JsonProperty(nameof(StatusDate))]
        public DateTime StatusDate { get; set; }
    }
}
