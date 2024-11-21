using Azure.Identity;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Middleware;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Api;

[ExcludeFromCodeCoverage]
public class HostBuilderConfiguration
{
    public IHost BuildHost()
    {
        return new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add HttpClient
        services.AddHttpClient();

        // Register services
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<INpwdClient, NpwdClient>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Add middleware
        services.AddTransient<NpwdOAuthMiddleware>();
        services.AddHttpClient(EprPrnIntegration.Common.Constants.HttpClientNames.Npwd)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>();

        // Configure Azure Key Vault
        ConfigureKeyVault(services);
    }

    private void ConfigureKeyVault(IServiceCollection services)
    {
        var keyVaultUrl = Environment.GetEnvironmentVariable(EprPrnIntegration.Common.Constants.ConfigSettingKeys.KeyVaultUrl) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            throw new ConfigurationErrorsException(EprPrnIntegration.Common.Constants.ConfigSettingKeys.KeyVaultUrl);
        }

        var appDirectory = AppContext.BaseDirectory;

        var config = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile(Path.Combine(appDirectory, "settings.json"), optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine(appDirectory, "local.settings.json"), optional: true, reloadOnChange: true)
            .AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential())
            .Build();

        services.Configure<Service>(config.GetSection("Service"));
    }
}