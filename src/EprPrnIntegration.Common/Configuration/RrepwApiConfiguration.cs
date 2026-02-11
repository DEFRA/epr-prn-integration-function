using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class RrepwApiConfiguration
{
    public const string SectionName = "RrepwApi";
    public string BaseUrl { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string AccessTokenUrl { get; set; } = null!;
    public string? Scope { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool UseStubbedData { get; set; }
    public string StubOrgId { get; set; } = null!;
    public string StubOrgIdComplianceScheme { get; set; } = null!;
}
