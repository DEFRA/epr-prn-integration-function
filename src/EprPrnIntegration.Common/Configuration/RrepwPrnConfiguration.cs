using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class RrepwPrnConfiguration
{
    public const string SectionName = "RrepwPrn";
    public string BaseUrl { get; set; } = null!;
    public int TimeoutSeconds { get; set; } = 30;
}
