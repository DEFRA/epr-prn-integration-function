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
        var clientIds = new List<string>();

        if (!string.IsNullOrEmpty(config.Value.PrnClientId))
            clientIds.Add(config.Value.PrnClientId);

        if (!string.IsNullOrEmpty(config.Value.AccountClientId))
            clientIds.Add(config.Value.AccountClientId);

        if (clientIds.Count == 0)
            return;

        _tokenRequestContext = new TokenRequestContext(clientIds.ToArray());
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