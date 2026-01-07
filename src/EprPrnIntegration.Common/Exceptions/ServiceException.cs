using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace EprPrnIntegration.Common.Exceptions;

[ExcludeFromCodeCoverage]
public class ServiceException : Exception
{
    public HttpStatusCode? StatusCode { get; private set; }

    public ServiceException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public ServiceException(string message, HttpStatusCode? statusCode, Exception exception)
        : base(message, exception)
    {
        StatusCode = statusCode;
    }
}
