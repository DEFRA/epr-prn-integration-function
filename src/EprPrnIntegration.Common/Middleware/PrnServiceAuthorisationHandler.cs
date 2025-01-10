using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.Middleware;

[ExcludeFromCodeCoverage]
public class PrnServiceAuthorisationHandler : DelegatingHandler
{
    private readonly TokenRequestContext _tokenRequestContext;
    private readonly DefaultAzureCredential? _credentials;

    public PrnServiceAuthorisationHandler(IOptions<Configuration.Service> config)
    {
        if (!string.IsNullOrEmpty(config.Value.ClientId))
        {
            _tokenRequestContext = new TokenRequestContext([config.Value.ClientId]);
            _credentials = new DefaultAzureCredential();
        }
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