using System.Text;
using EprPrnIntegration.Common.Service;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Client;

public class NpwdClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationService configurationService) : INpwdClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Constants.HttpClientNames.NpwdPush);

    public async Task<HttpResponseMessage> Patch<T>(T dataModel, string path)
    {
        var data = JsonConvert.SerializeObject(dataModel);
        var requestContent = new StringContent(data, Encoding.UTF8, "application/json");

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