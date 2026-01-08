using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EprPrnIntegration.Common.RESTServices;

/// <summary>
/// HTTP service base class that:
/// - Returns HttpResponseMessage to let Polly handle retries on status codes
/// - Does not throw exceptions for HTTP status code failures
/// - Lets HttpRequestException (network failures) propagate unchanged
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseHttpService
{
    protected readonly string _baseUrl;
    protected readonly HttpClient _httpClient;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    protected BaseHttpService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        string baseUrl,
        ILogger logger,
        string httpClientName = "",
        int timeoutSeconds = 100
    )
    {
        _httpContextAccessor =
            httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _httpClient = string.IsNullOrWhiteSpace(httpClientName)
            ? httpClientFactory.CreateClient()
            : httpClientFactory.CreateClient(httpClientName);

        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add(Constants.HttpHeaderNames.Accept, "application/json");

        _baseUrl = baseUrl;
    }

    protected void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
    }

    #region HTTP Methods

    protected Task<HttpResponseMessage> GetAsync(
        string path,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Get, path, null, cancellationToken);

    protected Task<HttpResponseMessage> PostAsync(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Post, path, payload, cancellationToken);

    protected Task<HttpResponseMessage> PutAsync(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Put, path, payload, cancellationToken);

    protected Task<HttpResponseMessage> DeleteAsync(
        string path,
        object? payload = null,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Delete, path, payload, cancellationToken);

    #endregion

    #region Core Send Methods

    /// <summary>
    /// Core method that sends an HTTP request.
    /// Returns HttpResponseMessage directly - caller must check IsSuccessStatusCode.
    /// HttpRequestException (network failures) propagate unchanged.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var url = BuildUrl(path);
        var request = new HttpRequestMessage(method, url);

        if (payload != null)
        {
            request.Content = JsonContent.Create(payload);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "HTTP {Method} to {Url} failed with {StatusCode}: {ResponseBody}",
                method,
                url,
                (int)response.StatusCode,
                responseBody
            );
        }

        return response;
    }

    // todo unit test this, why is it needed?
    private string BuildUrl(string path)
    {
        // Use Uri to properly join base URL with path
        var baseUri = new Uri(_baseUrl.TrimEnd('/') + "/");

        if (string.IsNullOrWhiteSpace(path))
            return baseUri.ToString();

        var trimmedPath = path.TrimStart('/');
        var result = new Uri(baseUri, trimmedPath);

        // Add trailing slash for paths without query strings (API convention)
        if (string.IsNullOrEmpty(result.Query) && !result.AbsolutePath.EndsWith('/'))
            return result.ToString().TrimEnd('/') + "/";

        return result.ToString();
    }

    #endregion
}
