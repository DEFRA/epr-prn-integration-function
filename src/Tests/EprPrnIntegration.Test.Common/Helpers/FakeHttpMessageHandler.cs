using EprPrnIntegration.Api.Models;
using System.Text.Json;

namespace EprPrnIntegration.Test.Common.Helpers
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<PersonEmail> _response;

        public FakeHttpMessageHandler(List<PersonEmail> response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var jsonResponse = JsonSerializer.Serialize(_response);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };

            return Task.FromResult(httpResponse);
        }
    }


}
