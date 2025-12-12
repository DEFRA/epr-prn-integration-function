using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.Middleware;

public class WasteOrganisationsApiAuthorisationHandler(
    IOptions<WasteOrganisationsApiConfiguration> config,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    ILogger<WasteOrganisationsApiAuthorisationHandler> logger)
    : DelegatingHandler
{
    private readonly WasteOrganisationsApiConfiguration _config = config.Value;
    private const string CacheKey = "WasteOrganisationsApiToken";

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

    private Task<string> GetCognitoTokenAsync(CancellationToken cancellationToken)
    {
        // GetOrCreateAsync is thread-safe and handles thundering herd prevention
        return memoryCache.GetOrCreateAsync(CacheKey, async entry =>
        {
            logger.LogInformation("Obtaining fresh Cognito access token");
            var tokenResponse = await FetchCognitoTokenAsync(cancellationToken);

            // Set cache expiration based on token lifetime (with 90% buffer for safety)
            var expirationTime = TimeSpan.FromSeconds(tokenResponse.ExpiresIn * 0.9);
            entry.AbsoluteExpirationRelativeToNow = expirationTime;

            logger.LogInformation("Cached Cognito access token (expires in {Seconds} seconds)", expirationTime.TotalSeconds);
            return tokenResponse.AccessToken!;
        })!;
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
