using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class ApiCallsRetryConfig
{
    public const string SectioName = "ApiCallsRetryConfig";
    public int? WaitTimeBetweenRetryInSecs { get; set; }
    public int? MaxAttempts { get; set; }
}