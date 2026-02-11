using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EprPrnIntegration.Common.Configuration;
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
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

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

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the semaphore
            cachedToken = memoryCache.Get<string>(tokenCacheKey);
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
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<CognitoTokenResponse> FetchCognitoTokenAsync(
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Fetching Cognito access token");
        var clientCredentials = $"{config.ClientId}:{config.ClientSecret}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientCredentials));

        var httpClient = httpClientFactory.CreateClient(Constants.HttpClientNames.CognitoToken);

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, config.AccessTokenUrl);
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            encodedCredentials
        );

        var formData = new Dictionary<string, string> { { "grant_type", "client_credentials" } };

        if (!string.IsNullOrEmpty(config.Scope))
        {
            formData.Add("scope", config.Scope);
        }

        tokenRequest.Content = new FormUrlEncodedContent(formData);

        var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<CognitoTokenResponse>(responseContent);

        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Failed to retrieve access token from Cognito");
        }

        logger.LogInformation("Successfully obtained Cognito access token");
        return tokenResponse;
    }

    private sealed class CognitoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
