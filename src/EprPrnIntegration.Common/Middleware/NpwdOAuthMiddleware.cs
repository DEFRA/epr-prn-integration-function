using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using EprPrnIntegration.Common.Service;

namespace EprPrnIntegration.Common.Middleware
{
    public class NpwdOAuthMiddleware : DelegatingHandler
    {
        private readonly IConfigurationService _configurationService;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        private readonly ILogger<NpwdOAuthMiddleware> _logger;
        private string? _accessToken;

        public NpwdOAuthMiddleware(IConfigurationService configurationService, ILogger<NpwdOAuthMiddleware> logger)
        {
            _logger = logger;
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            try
            {
                _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(_configurationService.NpwdClientId)
                    .WithClientSecret(_configurationService.NpwdClientSecret)
                    .WithAuthority(_configurationService.NpwdAuthority)
                    .Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: ex.Message);
                throw new TypeInitializationException(fullTypeName: $"Failed to initialize {nameof(NpwdOAuthMiddleware)}", innerException: ex);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    var result = await _confidentialClientApplication.AcquireTokenForClient([_configurationService.NpwdScope])
                        .WithExtraQueryParameters(new Dictionary<string, string>()
                        {
                            {"resource", _configurationService.NpwdAccessTokenUrl! },
                        })
                        .ExecuteAsync(cancellationToken);

                    _accessToken = result.AccessToken;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, _accessToken);

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
