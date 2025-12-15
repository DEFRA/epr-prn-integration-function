using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.Middleware;

public class WasteOrganisationsApiAuthorisationHandler(
    IOptions<WasteOrganisationsApiConfiguration> config,
    IHttpClientFactory httpClientFactory,
    ILogger<WasteOrganisationsApiAuthorisationHandler> logger)
    : DelegatingHandler
{
    private readonly WasteOrganisationsApiConfiguration _config = config.Value;
    private static readonly SemaphoreSlim TokenSemaphore = new(1, 1);
    private static string? _cachedToken;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var token = await GetCognitoTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetCognitoTokenAsync(CancellationToken cancellationToken)
    {
        // Fast path: check cache without locking
        if (_cachedToken != null)
        {
            return _cachedToken;
        }

        // Slow path: acquire semaphore to prevent thundering herd
        //
        // Why SemaphoreSlim instead of IMemoryCache.GetOrCreateAsync()?
        // - IMemoryCache.GetOrCreateAsync() does NOT prevent thundering herd (known issue: aspnet/Caching#218)
        //   See: https://github.com/aspnet/Caching/issues/218
        // - Multiple concurrent requests would all execute the factory function
        // - SemaphoreSlim ensures only ONE token fetch happens for concurrent requests
        //
        // Why not use token expiration/IMemoryCache?
        // - Function lifetime (running on cron) is much shorter than token TTL (typically 1 hour)
        // - Simple static cache is sufficient for the duration of a single function execution
        // - Usage pattern: process a list and make multiple API requests per item
        //
        // Why no built-in AWS helper?
        // - Amazon.Extensions.CognitoAuthentication only supports user authentication, not OAuth client credentials
        // - Cognito requires non-standard Basic auth on token endpoint, incompatible with generic OAuth libraries
        // - The preferred Duende.IdentityModel (https://docs.duendesoftware.com/identitymodel/) requires commercial
        //   licensing (https://duendesoftware.com/products/identitymodel) for production use
        // - This double-checked locking + SemaphoreSlim pattern is the standard approach for async lazy initialization
        //   See: https://blog.stephencleary.com/2012/08/asynchronous-lazy-initialization.html
        await TokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring semaphore
            if (_cachedToken != null)
            {
                return _cachedToken;
            }

            logger.LogInformation("Obtaining fresh Cognito access token");
            var tokenResponse = await FetchCognitoTokenAsync(cancellationToken);
            _cachedToken = tokenResponse.AccessToken!;

            return _cachedToken;
        }
        finally
        {
            TokenSemaphore.Release();
        }
    }

    public static void ClearCachedToken()
    {
        _cachedToken = null;
    }

    private async Task<CognitoTokenResponse> FetchCognitoTokenAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching Cognito access token");
        var clientCredentials = $"{_config.ClientId}:{_config.ClientSecret}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientCredentials));

        var httpClient = httpClientFactory.CreateClient();

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _config.AccessTokenUrl);
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _config.ClientId },
            { "client_secret", _config.ClientSecret }
        };

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

    private class CognitoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
