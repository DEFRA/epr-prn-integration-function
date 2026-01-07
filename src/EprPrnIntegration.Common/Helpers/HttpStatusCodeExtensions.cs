using System.Net;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Helpers;

public static class HttpStatusCodeExtensions
{
    /// <summary>
    /// Determines whether an HTTP request exception represents a transient error that may succeed on retry.
    /// </summary>
    /// <param name="exception">The HTTP request exception to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the exception represents a transient error (5xx server errors, 408 Request Timeout, or 429 Too Many Requests);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsTransient(this HttpStatusCode? statusCode, ILogger logger)
    {
        logger.LogDebug(
            "{SC} {s}",
            statusCode,
            statusCode != null
                && (
                    statusCode.Value >= HttpStatusCode.InternalServerError
                    || statusCode.Value == HttpStatusCode.RequestTimeout
                    || statusCode.Value == HttpStatusCode.TooManyRequests
                )
        );
        return statusCode != null
            && (
                statusCode.Value >= HttpStatusCode.InternalServerError
                || statusCode.Value == HttpStatusCode.RequestTimeout
                || statusCode.Value == HttpStatusCode.TooManyRequests
            );
    }
}
