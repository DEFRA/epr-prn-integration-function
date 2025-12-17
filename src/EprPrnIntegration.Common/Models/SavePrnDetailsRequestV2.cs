using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class SavePrnDetailsRequestV2
{
    public required string SourceSystemId { get; set; }
    public string PrnNumber { get; set; } = "";
    public required int PrnStatusId { get; set; }
    public required string PrnSignatory { get; set; }
    public string? PrnSignatoryPosition { get; set; }
    public DateTime? StatusUpdatedOn { get; set; }
    public required string IssuedByOrg { get; set; }
    public required Guid OrganisationId { get; set; }
    public required string OrganisationName { get; set; }
    public required string AccreditationNumber { get; set; }
    public required string AccreditationYear { get; set; }
    public string MaterialName { get; set; } = "";
    public required string ReprocessorExporterAgency { get; set; }
    public required string ReprocessingSite { get; set; }
    public required bool DecemberWaste { get; set; }
    public required bool IsExport { get; set; }
    public required int TonnageValue { get; set; }
    public string? IssuerNotes { get; set; }
}
