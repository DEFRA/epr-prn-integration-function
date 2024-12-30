﻿using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class Service
{
    public string? Url { get; set; }
    public string? EndPointName { get; set; }
    public string? BearerToken { get; set; }
    public string? HttpClientName { get; set; }
    public int? Retries { get; set; }
    public string? PrnBaseUrl { get; set; }
    public string? PrnEndPointName { get; set; }
    public string? CommonDataServiceBaseUrl { get; set; }
    public string? CommonDataServiceEndPointName { get; set; }
}