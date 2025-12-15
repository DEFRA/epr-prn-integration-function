using System.Net;

namespace EprPrnIntegration.Common.Helpers;

public static class HttpRequestExceptionExtensions
{
    /// <summary>
    /// Determines whether an HTTP request exception represents a transient error that may succeed on retry.
    /// </summary>
    /// <param name="exception">The HTTP request exception to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the exception represents a transient error (5xx server errors, 408 Request Timeout, or 429 Too Many Requests);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsTransient(this HttpRequestException exception)
    {
        return exception.StatusCode is >= HttpStatusCode.InternalServerError
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests;
    }
}
