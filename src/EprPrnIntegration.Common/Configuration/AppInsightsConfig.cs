using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class AppInsightsConfig
{
    public const string SectionName = "AppInsightsConfig";

    public string ResourceId { get; set; }
}
