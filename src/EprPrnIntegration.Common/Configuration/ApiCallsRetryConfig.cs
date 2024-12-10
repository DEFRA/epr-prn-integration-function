using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class ApiCallsRetryConfig
{
    public int? WaitTimeBetweenRetryInSecs { get; set; }
    public int? MaxAttempts { get; set; }
}