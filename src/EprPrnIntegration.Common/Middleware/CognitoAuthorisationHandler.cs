using System.Net.Http.Headers;
using EprPrnIntegration.Common.Configuration;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Middleware;

public class CognitoAuthorisationHandler(
    ICognitoConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger logger,
    IMemoryCache memoryCache,
    string tokenCacheKey
) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
        {
            throw new InvalidOperationException(
                "Cognito ClientId and ClientSecret must be configured"
            );
        }

        var token = await GetCognitoTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            Constants.HttpHeaderNames.Bearer,
            token
        );

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetCognitoTokenAsync(CancellationToken cancellationToken)
    {
        var cachedToken = memoryCache.Get<string>(tokenCacheKey);
        if (cachedToken != null)
        {
            return cachedToken;
        }

        logger.LogInformation(
            "Obtaining fresh Cognito access token for cache key: {CacheKey}",
            tokenCacheKey
        );
        var tokenResponse = await FetchCognitoTokenAsync(cancellationToken);
        var token = tokenResponse.AccessToken!;

        // Cache the token with expiration
        // Use 90% of token lifetime to ensure we refresh before actual expiration
        var fullExpiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn);
        var cacheExpiration = TimeSpan.FromSeconds(fullExpiration.TotalSeconds * 0.9);

        memoryCache.Set(tokenCacheKey, token, cacheExpiration);

        return token;
    }

    private async Task<TokenResponse> FetchCognitoTokenAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching Cognito access token");

        var httpClient = httpClientFactory.CreateClient(Constants.HttpClientNames.CognitoToken);

        var response = await httpClient.RequestClientCredentialsTokenAsync(
            new ClientCredentialsTokenRequest
            {
                Address = config.AccessTokenUrl,
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader,
            },
            cancellationToken
        );

        if (response.IsError)
        {
            throw response.Exception
                ?? new InvalidOperationException(
                    $"Failed to retrieve access token from Cognito: {response.Error}"
                );
        }

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new InvalidOperationException("Failed to retrieve access token from Cognito");
        }

        logger.LogInformation("Successfully obtained Cognito access token");
        return response;
    }
}
