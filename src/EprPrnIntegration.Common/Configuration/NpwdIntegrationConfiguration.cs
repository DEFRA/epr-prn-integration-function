﻿using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class NpwdIntegrationConfiguration
{
    public const string SectionName = "NpwdIntegration";
    public string BaseUrl { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public string AccessTokenUrl { get; set; } = null!;
    public string Authority { get; set; } = null!;
    public int TimeoutSeconds { get; set; } = 310;
}
