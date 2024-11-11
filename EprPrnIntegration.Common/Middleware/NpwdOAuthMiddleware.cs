using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Azure.Identity;

namespace EprPrnIntegration.Common.Middleware
{
    public class NpwdOAuthMiddleware : DelegatingHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        ILogger<NpwdOAuthMiddleware> _logger;
        private string? _accessToken;
        private string? _scope = null;
        private string? _accessTokenUrl = null;
        private string? _tokenName = null;

        public NpwdOAuthMiddleware(IConfiguration configuration, ILogger<NpwdOAuthMiddleware> logger)
        {
            _logger = logger;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var keyVaultUrl = _configuration["AzureKeyVaultUrl"];

            try
            {
                var kvClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                var kvClientId = kvClient.GetSecret("NPWDIntegrationClientID");

                var clientId = kvClient.GetSecret("NPWDIntegrationClientID")?.Value?.Value;
                var clientSecret = kvClient.GetSecret("NPWDIntegrationClientSecret")?.Value?.Value;
                var authority = kvClient.GetSecret("NPWDAuthority")?.Value?.Value;
                _scope = kvClient.GetSecret("NPWDScope")?.Value?.Value;
                _accessTokenUrl = kvClient.GetSecret("NPWDAccessTokenURL")?.Value?.Value;
                _tokenName = kvClient.GetSecret("NPWDTokenName")?.Value?.Value;

                _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri("https://npwdorguk.ciamlogin.com/638f082d-9a35-45fd-b98c-1d27072d70e"))
                    .Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(message: $"Failed to get one or more secrets from KeyVault : {keyVaultUrl}", exception: ex);
                throw new TypeInitializationException(fullTypeName: $"Failed to initialize {nameof(NpwdOAuthMiddleware)}", innerException: ex);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    var result = await _confidentialClientApplication.AcquireTokenForClient([_scope])
                        .WithExtraQueryParameters(new Dictionary<string, string>()
                        {
                            {"resource", _accessTokenUrl },
                        })
                        .ExecuteAsync(cancellationToken);

                    _accessToken = result.AccessToken;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                return await base.SendAsync(request, cancellationToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(message: ex.Message, exception: ex);
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}
