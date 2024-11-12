using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class PrnFunction
    {
        private readonly ILogger<PrnFunction> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly HttpClient _httpClient;

        public PrnFunction(IHttpClientFactory httpClientFactory, IConfigurationService configurationService, ILogger<PrnFunction> logger)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(Constants.HttpClientNames.Npwd);
            _configurationService = configurationService;
        }

        [Function("PrnFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                var baseAddress = _configurationService.GetNpwdApiBaseUrl();

                _httpClient.BaseAddress =  new Uri(baseAddress!);

                var response = await _httpClient.GetAsync("oData/PRNs");
                response.EnsureSuccessStatusCode();

                var data = response.Content.ReadAsStringAsync();
                return new OkObjectResult(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(message: ex.Message, exception: ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
