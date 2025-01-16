using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Middleware;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.CommonService;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.Service;
using EprPrnIntegration.Common.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Client;
using Notify.Interfaces;
using Polly;
using Polly.Extensions.Http;
using System.Diagnostics.CodeAnalysis;
using System.Net;

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
        services.AddScoped<ICommonDataService, CommonDataService>();
        services.AddScoped<IPrnService, PrnService>();
        services.AddScoped<INpwdClient, NpwdClient>();
        services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddScoped<IUtilities, Utilities>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAppInsightsService, AppInsightsService>();

        // Add the Notification Client
        services.AddSingleton<INotificationClient>(provider =>
        {
            MessagingConfig messagingConfig = new();
            configuration.GetSection(MessagingConfig.SectionName).Bind(messagingConfig);

            return new NotificationClient(messagingConfig.ApiKey);
        });

        // Add middleware
        services.AddHttpClients(configuration);
        services.AddServiceBus(configuration);
        services.ConfigureOptions(configuration);

        // Add the Notification Client
        services.AddSingleton<INotificationClient>(provider =>
        {
            var apiKey = configuration.GetValue<string>("MessagingConfig:ApiKey");
            return new NotificationClient(apiKey);
        });

        services.AddValidatorsFromAssemblyContaining<NpwdPrnValidator>();
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<NpwdOAuthMiddleware>();
        services.AddTransient<PrnServiceAuthorisationHandler>();
        services.AddTransient<OrganisationServiceAuthorisationHandler>();
        services.AddTransient<CommonDataServiceAuthorisationHandler>();

        // Add retry resilience policy
        ApiCallsRetryConfig apiCallsRetryConfig = new();
        configuration.GetSection(ApiCallsRetryConfig.SectioName).Bind(apiCallsRetryConfig);

        services.AddHttpClient(Common.Constants.HttpClientNames.Prn).AddHttpMessageHandler<PrnServiceAuthorisationHandler>()
            .AddPolicyHandler((services, request) =>
                GetRetryPolicy(services.GetService<ILogger<IPrnService>>()!, apiCallsRetryConfig?.MaxAttempts ?? 3, apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30, Common.Constants.HttpClientNames.Prn));
        
        services.AddHttpClient(Common.Constants.HttpClientNames.Organisation).AddHttpMessageHandler<OrganisationServiceAuthorisationHandler>()
        .AddPolicyHandler((services, request) =>
                GetRetryPolicy(services.GetService<ILogger<IOrganisationService>>()!, apiCallsRetryConfig?.MaxAttempts ?? 3, apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30, Common.Constants.HttpClientNames.Organisation));

        services.AddHttpClient(Common.Constants.HttpClientNames.CommonData).AddHttpMessageHandler<CommonDataServiceAuthorisationHandler>()
        .AddPolicyHandler((services, request) =>
                GetRetryPolicy(services.GetService<ILogger<ICommonDataService>>()!, apiCallsRetryConfig?.MaxAttempts ?? 3, apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30, Common.Constants.HttpClientNames.CommonData));

        services.AddHttpClient(Common.Constants.HttpClientNames.Npwd)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>()
            .AddPolicyHandler((services, request) =>
                GetRetryPolicy(services.GetService<ILogger<INpwdClient>>()!, apiCallsRetryConfig?.MaxAttempts ?? 3, apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30, "npwd"));

        return services;
    }

    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusConfiguration>(configuration.GetSection(ServiceBusConfiguration.SectionName));
        services.Configure<NpwdIntegrationConfiguration>(configuration.GetSection(NpwdIntegrationConfiguration.SectionName));
        services.Configure<Service>(configuration.GetSection("Service"));
        services.Configure<MessagingConfig>(configuration.GetSection("MessagingConfig"));
        services.Configure<FeatureManagementConfiguration>(configuration.GetSection(FeatureManagementConfiguration.SectionName));
        services.Configure<AppInsightsConfig>(configuration.GetSection(AppInsightsConfig.SectionName));
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
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger,int retryCount, double sleepDuration, string requestType)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(retryCount, (retryAttempt,res,ctx) =>
            {
                if (res.Result != null)
                {
                    var retryAfterHeader = res.Result.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "retry-after");
                    int retryAfter = 0;
                    if (res.Result.StatusCode == HttpStatusCode.TooManyRequests && retryAfterHeader.Value != null && retryAfterHeader.Value.Any())
                    {
                        retryAfter = int.Parse(retryAfterHeader.Value.First());
                        return TimeSpan.FromSeconds(retryAfter);
                    }
                }
                return TimeSpan.FromSeconds(sleepDuration);
            }, 
            async (response, timespan, retryAttempt, context) =>
            {
                logger
                .LogWarning("Retry attempt {retryAttempt} for service {requestType} with delay {delay} seconds as previuos request was responded with {StatusCode}",
                retryAttempt, requestType, timespan.TotalSeconds, response.Result?.StatusCode);
                await Task.CompletedTask;
            });
    }
}