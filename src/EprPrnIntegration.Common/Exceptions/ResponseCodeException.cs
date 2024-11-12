using System.Net;

namespace EprPrnIntegration.Common.Exceptions
{
    public class ResponseCodeException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public ResponseCodeException(
            HttpStatusCode statusCode,
            string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public ResponseCodeException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
    }
}
