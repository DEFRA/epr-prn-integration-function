using System.Text;
using EprPrnIntegration.Common.Service;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Client;

public class NpwdClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationService configurationService) : INpwdClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Common.Constants.HttpClientNames.Npwd);

    public async Task<HttpResponseMessage> Patch<T>(T dataModel, string path)
    {
        var producersData = JsonConvert.SerializeObject(dataModel);
        var requestContent = new StringContent(producersData, Encoding.UTF8, "application/json");

        var baseAddress = configurationService.GetNpwdApiBaseUrl();
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new UriFormatException("Base address for NPWD API is null or empty.");
        }

        _httpClient.BaseAddress = new Uri(baseAddress!);

        var response = await _httpClient.PatchAsync(path, requestContent);
        return response;
    }
}