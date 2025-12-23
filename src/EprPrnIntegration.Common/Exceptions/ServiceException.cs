using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Exceptions;

[ExcludeFromCodeCoverage]
public class ServiceException : Exception
{
    public ServiceException(string message)
        : base(message) { }

    public ServiceException(string message, Exception _exception)
        : base(message, _exception) { }
}
