using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class SavePrnDetailsRequest
{
    public string? SourceSystemId { get; set; }
    public string? PrnNumber { get; set; }
    public int? PrnStatusId { get; set; }
    public string? PrnSignatory { get; set; }
    public string? PrnSignatoryPosition { get; set; }
    public DateTime? StatusUpdatedOn { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? IssuedByOrg { get; set; }
    public Guid? OrganisationId { get; set; }
    public string? OrganisationName { get; set; }
    public string? AccreditationNumber { get; set; }
    public string? AccreditationYear { get; set; }
    public string? MaterialName { get; set; }
    public string? ReprocessorExporterAgency { get; set; }
    public string? ReprocessingSite { get; set; }
    public bool? DecemberWaste { get; set; }
    public bool? IsExport { get; set; }
    public int? TonnageValue { get; set; }
    public string? IssuerNotes { get; set; }
    public string? ProcessToBeUsed { get; set; }
    public string? ObligationYear { get; set; }
}
