using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.RESTServices;

/// <summary>
/// A cleaner HTTP service base class that:
/// - Has a single SendAsync method to avoid duplication
/// - Does not catch and wrap exceptions unnecessarily
/// - Lets HttpRequestException (network failures) propagate unchanged
/// - Only throws ServiceException for non-success HTTP status codes
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

    protected Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    protected Task<T> PostAsync<T>(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync<T>(HttpMethod.Post, path, payload, cancellationToken);

    protected Task PostAsync(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Post, path, payload, cancellationToken);

    protected Task<T> PutAsync<T>(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync<T>(HttpMethod.Put, path, payload, cancellationToken);

    protected Task PutAsync(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Put, path, payload, cancellationToken);

    protected Task<T> DeleteAsync<T>(
        string path,
        object? payload = null,
        CancellationToken cancellationToken = default
    ) => SendAsync<T>(HttpMethod.Delete, path, payload, cancellationToken);

    protected Task DeleteAsync(
        string path,
        object? payload = null,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Delete, path, payload, cancellationToken);

    #endregion

    #region Core Send Methods

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var response = await SendCoreAsync(method, path, payload, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
            return default!;

        return JsonConvert.DeserializeObject<T>(content)!;
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        await SendCoreAsync(method, path, payload, cancellationToken);
    }

    /// <summary>
    /// Core method that sends an HTTP request.
    /// - HttpRequestException (network failures) propagate unchanged
    /// - Non-success status codes throw ServiceException with the status code
    /// </summary>
    private async Task<HttpResponseMessage> SendCoreAsync(
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
            _logger.LogError(
                "HTTP {Method} to {Url} failed with {StatusCode}: {ResponseBody}",
                method,
                url,
                (int)response.StatusCode,
                responseBody
            );
            if (response.StatusCode.IsTransient())
            {
                throw new HttpRequestTransientException(
                    "HTTP {Method} to {Url} failed with {StatusCode}: {ResponseBody}",
                    null,
                    response.StatusCode
                );
            }
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

public class HttpRequestTransientException : HttpRequestException
{
    public HttpRequestTransientException() { }

    public HttpRequestTransientException(
        string? message,
        Exception? inner,
        HttpStatusCode? statusCode
    )
        : base(message, inner, statusCode) { }
}
