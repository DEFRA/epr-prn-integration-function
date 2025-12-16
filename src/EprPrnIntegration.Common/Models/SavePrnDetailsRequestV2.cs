namespace EprPrnIntegration.Common.Models;

/// <summary>
/// Don't change this unless you also change the version in the Common Data API
/// </summary>
public class SavePrnDetailsRequestV2
{
    public required string AccreditationNumber { get; set; }
    public required string AccreditationYear { get; set; }
    public bool DecemberWaste { get; set; }
    public required string MaterialName { get; set; }
    public required string PrnNumber { get; set; }
    public int PrnStatusId { get; set; }
    public int TonnageValue { get; set; }
    public DateTime IssueDate { get; set; }
    public required string IssuedByOrg { get; set; }
    public required string OrganisationName { get; set; }
    public required Guid OrganisationId { get; set; }
    public required string PackagingProducer { get; set; }
    public string? ProcessToBeUsed { get; set; }
    public required string ReprocessorExporterAgency { get; set; }
    public DateTime StatusUpdatedOn { get; set; }
    public string? IssuerNotes { get; set; }
    public required string? IssuerReference { get; set; }
    public string? PrnSignatory { get; set; }
    public string? PrnSignatoryPosition { get; set; }
    public required string CreatedBy { get; set; }
    public required string SourceSystemId { get; set; }
    public required string ProducerAgency { get; set; }
    public required Guid ExternalId { get; set; }
    public string? Signature { get; set; }
    public required string ReprocessingSite { get; set; }
    public bool IsExport { get; set; }
}