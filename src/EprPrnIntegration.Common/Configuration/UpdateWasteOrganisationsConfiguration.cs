using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class UpdateWasteOrganisationsConfiguration
{
    public const string SectionName = "UpdateWasteOrganisations";
    public string Trigger { get; set; } = null!;
    public string DefaultStartDate { get; set; } = null!;
}
