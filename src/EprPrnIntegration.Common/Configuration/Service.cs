using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class Service
{
    public string? AccountBaseUrl { get; set; }
    public string? AccountEndPointName { get; set; }
    public string? BearerToken { get; set; }
    public string? HttpClientName { get; set; }
    public int? Retries { get; set; }
    public string? PrnBaseUrl { get; set; }
    public string? PrnEndPointName { get; set; }
    public string? AccountClientId { get; set; }
    public string? PrnClientId { get; set; }
    public string? CommonDataClientId { get; set; }
    public string? CommonDataServiceBaseUrl { get; set; }
    public string? CommonDataServiceEndPointName { get; set; }
}