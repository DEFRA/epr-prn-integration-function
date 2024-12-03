using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Middleware;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using EprPrnIntegration.Common.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Notify.Client;
using Notify.Interfaces;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

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

        // Add HttpClient
        services.AddHttpClient();

        // Register services
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IPrnService, PrnService>();
        services.AddScoped<INpwdClient, NpwdClient>();
        services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IEmailService, EmailService>();

        // Add middleware
        services.AddTransient<NpwdOAuthMiddleware>();
        
        // Add HttpClients
        services.AddHttpClient(Common.Constants.HttpClientNames.Npwd)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>();

        services.AddServiceBus(configuration);
        services.ConfigureOptions(configuration);
        // Configure Azure Key Vault
        ConfigureKeyVault(configuration);
        
        // Add the Notification Client
        services.AddSingleton<INotificationClient>(provider =>
        {
            var apiKey = configuration.GetValue<string>("MessagingConfig:ApiKey");
            return new NotificationClient(apiKey);
        });

        services.AddValidatorsFromAssemblyContaining<NpwdPrnValidator>();
    }

    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusConfiguration>(configuration.GetSection(ServiceBusConfiguration.SectionName));
        services.Configure<Service>(configuration.GetSection("Service"));
        services.Configure<MessagingConfig>(configuration.GetSection("MessagingConfig"));
        return services;
    }

    public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        var isRunningLocally = configuration.GetValue<bool?>("IsRunningLocally");
        if (isRunningLocally is true)
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                    var sp = services.BuildServiceProvider();
                    var serviceBusConfig = sp.GetRequiredService<IOptions<ServiceBusConfiguration>>().Value;
                    return new(serviceBusConfig.ConnectionString, options);
                });
            });
        }
        else
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                    var sp = services.BuildServiceProvider();
                    var serviceBusConfig = sp.GetRequiredService<IOptions<ServiceBusConfiguration>>().Value;
                    return new(serviceBusConfig.FullyQualifiedNamespace, new DefaultAzureCredential(), options);
                });
            });
        }
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