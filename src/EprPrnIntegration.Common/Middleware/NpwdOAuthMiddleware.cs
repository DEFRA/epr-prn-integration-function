using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EprPrnIntegration.Common.Configuration;

namespace EprPrnIntegration.Common.Middleware;

[ExcludeFromCodeCoverage]
public class NpwdOAuthMiddleware : DelegatingHandler
{
    private readonly NpwdIntegrationConfiguration _npwdIntegrationConfig;
    private readonly IConfidentialClientApplication _confidentialClientApplication;
    private readonly ILogger<NpwdOAuthMiddleware> _logger;
    private string? _accessToken;

    public NpwdOAuthMiddleware(IOptions<NpwdIntegrationConfiguration> npwdConfig, ILogger<NpwdOAuthMiddleware> logger)
    {
        _logger = logger;
        _npwdIntegrationConfig = npwdConfig.Value;

        if (String.IsNullOrEmpty(_npwdIntegrationConfig.ClientId))
        {
            // this enables local dev to fake endpoints, without having to auth handshake to anything.
            _logger.LogWarning("Skipping NPWD auth middleware because no client id configured.");
            return;
        }

        try
        {
            _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(_npwdIntegrationConfig.ClientId)
                .WithClientSecret(_npwdIntegrationConfig.ClientSecret)
                .WithAuthority(_npwdIntegrationConfig.Authority)
                .Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NpwdOAuthMiddleware failed");
            throw new TypeInitializationException(fullTypeName: $"Failed to initialize {nameof(NpwdOAuthMiddleware)}", innerException: ex);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            if (_confidentialClientApplication == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }
            
            if (string.IsNullOrEmpty(_accessToken))
            {
                var result = await _confidentialClientApplication.AcquireTokenForClient([_npwdIntegrationConfig.Scope])
                    .WithExtraQueryParameters(new Dictionary<string, string>()
                    {
                        {"resource", _npwdIntegrationConfig.AccessTokenUrl },
                    })
                    .ExecuteAsync(cancellationToken);
                
                _accessToken = result.AccessToken;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, _accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "SendAsync failed");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
    }
}