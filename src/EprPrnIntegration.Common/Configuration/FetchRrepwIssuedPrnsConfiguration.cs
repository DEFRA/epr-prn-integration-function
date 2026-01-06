using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class FetchRrepwIssuedPrnsConfiguration
{
    public const string SectionName = FunctionName.FetchRrepwIssuedPrns;
    public string Trigger { get; set; } = null!;
    public string DefaultStartDate { get; set; } = null!;
}
