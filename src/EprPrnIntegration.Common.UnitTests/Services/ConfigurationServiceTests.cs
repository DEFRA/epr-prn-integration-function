using Azure;
using Azure.Security.KeyVault.Secrets;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Configuration;
using Constants = EprPrnIntegration.Common.Constants;

namespace EprPrnIntegration.Common.UnitTests.Services;

public class ConfigurationServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<SecretClient> _keyVaultClientMock = new();
    private const string ValidKeyVaultUrl = "https://testkeyvault.vault.azure.net/";

    public ConfigurationServiceTests()
    {
        _configurationMock.Setup(config => config[Constants.ConfigSettingKeys.KeyVaultUrl]).Returns(ValidKeyVaultUrl);
    }

    [Fact]
    public void Constructor_ValidKeyVaultUrl_DoesNotThrow()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_InvalidKeyVaultUrl_ThrowsConfigurationErrorsException()
    {
        // Arrange
        _configurationMock.Setup(config => config[Constants.ConfigSettingKeys.KeyVaultUrl]).Returns("invalid-url");

        // Act & Assert
        Assert.Throws<ConfigurationErrorsException>(() => CreateService());
    }

    [Fact]
    public void Constructor_MissingKeyVaultUrl_ThrowsConfigurationErrorsException()
    {
        // Arrange
        _configurationMock.Setup(config => config[Constants.ConfigSettingKeys.KeyVaultUrl]).Returns<string>(null);

        // Act & Assert
        Assert.Throws<ConfigurationErrorsException>(() => CreateService());
    }

    [Fact]
    public void GetNpwdApiBaseUrl_InvalidUrl_ThrowsConfigurationErrorsException()
    {
        // Arrange
        _configurationMock.Setup(config => config[Constants.ConfigSettingKeys.NpwdOAuth.ApiBaseUrl]).Returns("invalid-url");
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ConfigurationErrorsException>(() => service.GetNpwdApiBaseUrl());
    }

    [Fact]
    public void GetNpwdApiBaseUrl_ValidUrl_ReturnsUrl()
    {
        // Arrange
        var validApiUrl = "https://testapi.com/";
        _configurationMock.Setup(config => config[Constants.ConfigSettingKeys.NpwdOAuth.ApiBaseUrl]).Returns(validApiUrl);
        var service = CreateService();

        // Act
        var result = service.GetNpwdApiBaseUrl();

        // Assert
        Assert.Equal(validApiUrl, result);
    }

    [Fact]
    public void NpwdClientId_ReturnsSecretValue()
    {
        // Arrange
        var secretName = Constants.ConfigSettingKeys.NpwdOAuth.ClientId;
        var secretValue = "client-id-value";

        MockKeyVaultSecret(secretName, secretValue);
        var service = CreateService();

        // Act
        var result = service.NpwdClientId;

        // Assert
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public void NpwdClientSecret_ReturnsSecretValue()
    {
        // Arrange
        var secretName = Constants.ConfigSettingKeys.NpwdOAuth.ClientSecret;
        var secretValue = "client-secret-value";

        MockKeyVaultSecret(secretName, secretValue);
        var service = CreateService();

        // Act
        var result = service.NpwdClientSecret;

        // Assert
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public void NpwdAuthority_ReturnsSecretValue()
    {
        // Arrange
        var secretName = Constants.ConfigSettingKeys.NpwdOAuth.Authority;
        var secretValue = "https://authority.example.com/";

        MockKeyVaultSecret(secretName, secretValue);
        var service = CreateService();

        // Act
        var result = service.NpwdAuthority;

        // Assert
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public void NpwdScope_ReturnsSecretValue()
    {
        // Arrange
        var secretName = Constants.ConfigSettingKeys.NpwdOAuth.Scope;
        var secretValue = "api.scope";

        MockKeyVaultSecret(secretName, secretValue);
        var service = CreateService();

        // Act
        var result = service.NpwdScope;

        // Assert
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public void NpwdAccessTokenUrl_ReturnsSecretValue()
    {
        // Arrange
        var secretName = Constants.ConfigSettingKeys.NpwdOAuth.AccessTokenUrl;
        var secretValue = "https://tokenurl.example.com/";

        MockKeyVaultSecret(secretName, secretValue);
        var service = CreateService();

        // Act
        var result = service.NpwdAccessTokenUrl;

        // Assert
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public void NpwdAccessTokenName_ReturnsSecretValue()
    {
        // Arrange
        var secretName = Constants.ConfigSettingKeys.NpwdOAuth.TokenName;
        var secretValue = "access_token";

        MockKeyVaultSecret(secretName, secretValue);
        var service = CreateService();

        // Act
        var result = service.NpwdAccessTokenName;

        // Assert
        Assert.Equal(secretValue, result);
    }

    private void MockKeyVaultSecret(string secretName, string secretValue)
    {
        var secret = new KeyVaultSecret(secretName, secretValue);

        var responseMock = new Mock<Response<KeyVaultSecret>>();
        responseMock.Setup(r => r.Value).Returns(secret);

        _keyVaultClientMock
            .Setup(client => client.GetSecret(secretName, null, default))
            .Returns(responseMock.Object);
    }

    private ConfigurationService CreateService() => new(_configurationMock.Object, _keyVaultClientMock.Object);
}
