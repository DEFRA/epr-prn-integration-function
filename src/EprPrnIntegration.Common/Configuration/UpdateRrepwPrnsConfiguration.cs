using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class UpdateRrepwPrnsConfiguration
{
    public static string SectionName => FunctionName.UpdateRrepwPrnsList;
    public string Trigger { get; set; } = null!;
    public string DefaultStartDate { get; set; } = null!;
    public int UpdateRrepwPrnsMaxRows { get; set; } = int.MaxValue;
}
