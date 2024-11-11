using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EprPrnIntegration.Test.Common.Helpers
{
    public class HttpClientFactoryMock : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public HttpClientFactoryMock(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }
    }
}
