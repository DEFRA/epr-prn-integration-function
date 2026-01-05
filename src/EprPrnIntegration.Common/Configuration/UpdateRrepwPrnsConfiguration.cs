using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class UpdateRrepwPrnsConfiguration
{
    public const string SectionName = "UpdateRrepwPrns";
    public string Trigger { get; set; } = null!;
    public string DefaultStartDate { get; set; } = null!;
    public int UpdateRrepwPrnsMaxRows { get; set; } = int.MaxValue;
}
