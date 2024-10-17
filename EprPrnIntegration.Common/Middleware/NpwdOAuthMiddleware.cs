using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace EprPrnIntegration.Common.Middleware
{
    public class NpwdOAuthMiddleware : DelegatingHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        private string? _accessToken;

        public NpwdOAuthMiddleware(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var authority = _configuration["AzureAd:Authority"];

            _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(authority))                
                .Build();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_accessToken) || TokenExpired())
            {
                var result = await _confidentialClientApplication.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                    .ExecuteAsync(cancellationToken);

                _accessToken = result.AccessToken;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private bool TokenExpired()
        {
            // Implement token expiration check logic here
            return false;
        }
    }

}
