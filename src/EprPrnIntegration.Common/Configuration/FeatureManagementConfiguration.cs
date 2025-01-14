using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class FeatureManagementConfiguration
{
    public const string SectionName = "FeatureManagement";

    public bool? RunIntegration { get; set; }

    public bool? RunReconciliation { get; set; }
}
