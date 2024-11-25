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
using System.Net.Security;

namespace EprPrnIntegration.Api;
[ExcludeFromCodeCoverage]
public static class HostBuilderConfiguration
{
    public static IHost BuildHost()
    {
        return new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((hostingContext, services) =>
                ConfigureServices(hostingContext.Configuration, services))
            .Build();
    }
    private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
       
        // Register services
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<INpwdClient, NpwdClient>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Add middleware
        services.AddTransient<NpwdOAuthMiddleware>();
        
        // Add HttpClients
        services.AddHttpClient(Common.Constants.HttpClientNames.NpwdGet)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>();

        services.AddHttpClient(Common.Constants.HttpClientNames.NpwdPush)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" || errors == SslPolicyErrors.None
                };
            });

        // Add configuration
        services.ConfigureOptions(configuration);
        // Configure Azure Key Vault
        ConfigureKeyVault(configuration);
    }

    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Service>(configuration.GetSection("Service"));
        return services;
    }
    private static void ConfigureKeyVault(IConfiguration configuration)
    {
        var keyVaultUrl = configuration.GetValue<string?>(Common.Constants.ConfigSettingKeys.KeyVaultUrl);
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            throw new ConfigurationErrorsException(Common.Constants.ConfigSettingKeys.KeyVaultUrl);
        }
    }
}