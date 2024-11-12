using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace EprPrnIntegration.Common.Service
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly SecretClient _keyVaultClient;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;

            var keyVaultUrl = _configuration[Constants.ConfigSettingKeys.KeyVaultUrl]
                ?? throw new ConfigurationErrorsException(Constants.ConfigSettingKeys.KeyVaultUrl);

            if (!Uri.IsWellFormedUriString(keyVaultUrl, UriKind.Absolute))
                throw new ConfigurationErrorsException(Constants.ConfigSettingKeys.KeyVaultUrl);

            _keyVaultClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }


        public string? NpwdClientId => _keyVaultClient?.GetSecret(Constants.ConfigSettingKeys.NpwdOAuth.ClientId)?.Value?.Value;

        public string? NpwdClientSecret => _keyVaultClient?.GetSecret(Constants.ConfigSettingKeys.NpwdOAuth.ClientSecret)?.Value?.Value;

        public string? NpwdAuthority => _keyVaultClient?.GetSecret(Constants.ConfigSettingKeys.NpwdOAuth.Authority)?.Value?.Value;

        public string? NpwdScope => _keyVaultClient.GetSecret(Constants.ConfigSettingKeys.NpwdOAuth.Scope)?.Value?.Value;

        public string? NpwdAccessTokenUrl => _keyVaultClient?.GetSecret(Constants.ConfigSettingKeys.NpwdOAuth.AccessTokenUrl)?.Value?.Value;

        public string? NpwdAccessTokenName => _keyVaultClient?.GetSecret(Constants.ConfigSettingKeys.NpwdOAuth.TokenName)?.Value?.Value;

        public string? GetNpwdApiBaseUrl()
        {
            var url = _configuration[Constants.ConfigSettingKeys.NpwdOAuth.ApiBaseUrl];

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ConfigurationErrorsException($"Invalid configuration data for Npwd API Url : {Constants.ConfigSettingKeys.NpwdOAuth.ApiBaseUrl}");

            return url;
        }
    }
}
