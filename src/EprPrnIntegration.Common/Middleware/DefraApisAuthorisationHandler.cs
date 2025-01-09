using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using EprPrnIntegration.Common.Service;
using Azure.Core;
using Azure.Identity;

namespace EprPrnIntegration.Common.Middleware;

[ExcludeFromCodeCoverage]
public class DefraApisAuthorisationHandler : DelegatingHandler
{
    private readonly TokenRequestContext _tokenRequestContext;
    private readonly DefaultAzureCredential? _credentials;

    public DefraApisAuthorisationHandler(IConfigurationService configurationService, ILogger<DefraApisAuthorisationHandler> logger)
    {
        //if (!string.IsNullOrEmpty(options.Value.ClientId))
        //{
        //    _tokenRequestContext = new TokenRequestContext([options.Value.ClientId]);
        //    _credentials = new DefaultAzureCredential();
        //}
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AddDefaultToken(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task AddDefaultToken(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_credentials != null)
        {
            var tokenResult = await _credentials.GetTokenAsync(_tokenRequestContext, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, tokenResult.Token);
        }
    }
}