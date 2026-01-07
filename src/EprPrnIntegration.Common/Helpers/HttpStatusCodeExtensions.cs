using System.Net;

namespace EprPrnIntegration.Common.Helpers;

public static class HttpStatusCodeExtensions
{
    public static bool IsTransient(this HttpStatusCode? statusCode)
    {
        return statusCode
            is >= HttpStatusCode.InternalServerError
                or HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests;
    }
}
