using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Middleware;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Notify.Client;
using Notify.Interfaces;
using Polly;
using Polly.Extensions.Http;
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

        // Register services
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IPrnService, PrnService>();
        services.AddScoped<INpwdClient, NpwdClient>();
        services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddScoped<IUtilities, Utilities>();
        services.AddScoped<IEmailService, EmailService>();

        // Add the Notification Client
        services.AddSingleton<INotificationClient>(provider =>
        {
            MessagingConfig messagingConfig = new();
            configuration.GetSection(MessagingConfig.SectionName).Bind(messagingConfig);

            return new NotificationClient(messagingConfig.ApiKey);
        });

        // Add middleware
        services.AddTransient<NpwdOAuthMiddleware>();

        // Add retry resilience policy
        ApiCallsRetryConfig apiCallsRetryConfig = new();
        configuration.GetSection(ApiCallsRetryConfig.SectioName).Bind(apiCallsRetryConfig);

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(apiCallsRetryConfig?.MaxAttempts ?? 3, retryAttempt => TimeSpan.FromSeconds(apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30));

        // Add HttpClients
        services.AddHttpClient();
        services.AddHttpClient(Common.Constants.HttpClientNames.Npwd)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>()
            .AddPolicyHandler(retryPolicy);
            
        services.AddServiceBus(configuration);
        services.ConfigureOptions(configuration);
        // Configure Azure Key Vault
        ConfigureKeyVault(configuration);
    }


    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusConfiguration>(configuration.GetSection(ServiceBusConfiguration.SectionName));
        services.Configure<Service>(configuration.GetSection("Service"));
        services.Configure<FeatureManagementConfiguration>(configuration.GetSection(FeatureManagementConfiguration.SectionName));
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