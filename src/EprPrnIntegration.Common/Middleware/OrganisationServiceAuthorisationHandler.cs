using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.Middleware;

[ExcludeFromCodeCoverage]
public class OrganisationServiceAuthorisationHandler : DelegatingHandler
{
    private readonly TokenRequestContext _tokenRequestContext;    
    private readonly DefaultAzureCredential? _credentials;

    public OrganisationServiceAuthorisationHandler(IOptions<Configuration.Service> config)
    {
        if (string.IsNullOrEmpty(config.Value.AccountClientId))
        {
            return;
        }

        _tokenRequestContext = new TokenRequestContext([config.Value.AccountClientId]);
        _credentials = new DefaultAzureCredential();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_credentials != null)
        {
            var tokenResult = await _credentials.GetTokenAsync(_tokenRequestContext, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, tokenResult.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}