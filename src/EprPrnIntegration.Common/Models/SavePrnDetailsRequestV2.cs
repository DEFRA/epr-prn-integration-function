using System.Diagnostics.CodeAnalysis;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace EprPrnIntegration.Common.Models;

[ExcludeFromCodeCoverage]
public class SavePrnDetailsRequestV2
{
    public required string SourceSystemId { get; set; }
    public required string PrnNumber { get; set; } 
    public required int PrnStatusId { get; set; }
    public required string PrnSignatory { get; set; }
    public string? PrnSignatoryPosition { get; set; }
    public required DateTime StatusUpdatedOn { get; set; }
    public required string IssuedByOrg { get; set; }
    public required Guid OrganisationId { get; set; }
    public required string OrganisationName { get; set; }
    public required string AccreditationNumber { get; set; }
    public required string AccreditationYear { get; set; }
    public required string MaterialName { get; set; }
    public required string ReprocessorExporterAgency { get; set; }
    public string? ReprocessingSite { get; set; }
    public required bool DecemberWaste { get; set; }
    public required bool IsExport { get; set; }
    public required int TonnageValue { get; set; }
    public string? IssuerNotes { get; set; }
    public required string ProcessToBeUsed { get; set; }
}
