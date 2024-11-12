namespace EprPrnIntegration.Common.Exceptions
{
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message)
        {
        }

        public ServiceException(string message, Exception _exception) : base(message, _exception)
        {
        }
    }
}
