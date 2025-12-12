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

    public static void ClearCachedToken()
    {
        _cachedToken = null;
    }

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
        // Fast path: check cache first without locking
        if (_cachedToken != null)
        {
            logger.LogInformation("Using cached Cognito access token");
            return _cachedToken;
        }

        // Slow path: acquire semaphore to prevent thundering herd
        await TokenSemaphore.WaitAsync();
        try
        {
            // Double-check cache after acquiring lock
            if (_cachedToken != null)
            {
                logger.LogInformation("Using cached Cognito access token (acquired after lock)");
                return _cachedToken;
            }

            // Fetch fresh token
            logger.LogInformation("Obtaining fresh Cognito access token");
            var token = await FetchCognitoTokenAsync(cancellationToken);
            _cachedToken = token;

            return token;
        }
        finally
        {
            TokenSemaphore.Release();
        }
    }

    private async Task<string> FetchCognitoTokenAsync(CancellationToken cancellationToken)
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

        logger.LogInformation($"Successfully obtained Cognito access token");
        return tokenResponse.AccessToken;
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
