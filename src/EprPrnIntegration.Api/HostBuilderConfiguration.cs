using System.Diagnostics.CodeAnalysis;
using System.Net;
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
using EprPrnIntegration.Common.RESTServices.PrnBackendService;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using EprPrnIntegration.Common.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Client;
using Notify.Interfaces;
using Polly;
using Polly.Extensions.Http;

namespace EprPrnIntegration.Api;

[ExcludeFromCodeCoverage]
public static class HostBuilderConfiguration
{
    public static IHost BuildHost()
    {
        return new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices(
                (hostingContext, services) =>
                    ConfigureServices(hostingContext.Configuration, services)
            )
            .Build();
    }

    private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Add Application Insights
        services.AddCustomApplicationInsights();

        // Add memory cache for token caching
        services.AddMemoryCache();

        // Register services
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<ICommonDataService, CommonDataService>();
        services.AddScoped<IPrnService, PrnService>();
        services.AddScoped<INpwdPrnService, NpwdPrnService>();
        services.AddScoped<INpwdClient, NpwdClient>();
        services.AddScoped<IServiceBusProvider, ServiceBusProvider>();
        services.AddScoped<IWasteOrganisationsService, WasteOrganisationsService>();

        // Register RRepw service - use stubbed version if configured
        RrepwApiConfiguration rrepwConfig = new();
        configuration.GetSection(RrepwApiConfiguration.SectionName).Bind(rrepwConfig);
        if (rrepwConfig.UseStubbedData)
        {
            services.AddScoped<IRrepwService, StubbedRrepwService>();
        }
        else
        {
            services.AddScoped<IRrepwService, RrepwService>();
        }
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddScoped<IUtilities, Utilities>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAppInsightsService, AppInsightsService>();

        services.AddScoped<IBlobStorage, BlobStorage>();
        services.AddScoped<ILastUpdateService, LastUpdateService>();

        // Add the Notification Client
        services.AddSingleton<INotificationClient>(provider =>
        {
            MessagingConfig messagingConfig = new();
            configuration.GetSection(MessagingConfig.SectionName).Bind(messagingConfig);

            if (messagingConfig.Bypass)
                return new PassThruNotificationClient(
                    provider.GetRequiredService<ILogger<INotificationClient>>()
                );

            return new NotificationClient(messagingConfig.ApiKey);
        });

        services.AddAzureClients(builder =>
        {
            builder.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        });

        // Add middleware
        services.AddHttpClients(configuration);
        services.AddServiceBus(configuration);
        services.ConfigureOptions(configuration);

        services.AddValidatorsFromAssemblyContaining<NpwdPrnValidator>();
        services.AddScoped<ICoreServices, CoreServices>();
        services.AddScoped<IMessagingServices, MessagingServices>();
    }

    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddTransient<NpwdOAuthMiddleware>();
        services.AddTransient<PrnServiceAuthorisationHandler>();
        services.AddTransient<OrganisationServiceAuthorisationHandler>();
        services.AddTransient<CommonDataServiceAuthorisationHandler>();

        // Add retry resilience policy
        ApiCallsRetryConfig apiCallsRetryConfig = new();
        configuration.GetSection(ApiCallsRetryConfig.SectioName).Bind(apiCallsRetryConfig);

        services
            .AddHttpClient(Common.Constants.HttpClientNames.Prn)
            .AddHttpMessageHandler<PrnServiceAuthorisationHandler>()
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<IPrnService>>()!,
                        apiCallsRetryConfig?.MaxAttempts ?? 3,
                        apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30,
                        Common.Constants.HttpClientNames.Prn
                    )
            );

        services
            .AddHttpClient(Common.Constants.HttpClientNames.PrnV2)
            .AddHttpMessageHandler<PrnServiceAuthorisationHandler>()
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<IPrnService>>()!,
                        apiCallsRetryConfig?.MaxAttempts ?? 3,
                        apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30,
                        Common.Constants.HttpClientNames.PrnV2
                    )
            );

        services
            .AddHttpClient(Common.Constants.HttpClientNames.Organisation)
            .AddHttpMessageHandler<OrganisationServiceAuthorisationHandler>()
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<IOrganisationService>>()!,
                        apiCallsRetryConfig?.MaxAttempts ?? 3,
                        apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30,
                        Common.Constants.HttpClientNames.Organisation
                    )
            );

        services
            .AddHttpClient(Common.Constants.HttpClientNames.CommonData)
            .AddHttpMessageHandler<CommonDataServiceAuthorisationHandler>()
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<ICommonDataService>>()!,
                        apiCallsRetryConfig?.MaxAttempts ?? 3,
                        apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30,
                        Common.Constants.HttpClientNames.CommonData
                    )
            );

        services
            .AddHttpClient(Common.Constants.HttpClientNames.Npwd)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>()
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<INpwdClient>>()!,
                        apiCallsRetryConfig?.MaxAttempts ?? 3,
                        apiCallsRetryConfig?.WaitTimeBetweenRetryInSecs ?? 30,
                        "npwd"
                    )
            );

        WasteOrganisationsApiConfiguration wasteOrganisationsApiConfig = new();
        configuration
            .GetSection(WasteOrganisationsApiConfiguration.SectionName)
            .Bind(wasteOrganisationsApiConfig);
        services
            .AddHttpClient(Common.Constants.HttpClientNames.WasteOrganisations)
            .AddHttpMessageHandler(sp =>
            {
                var config = sp.GetRequiredService<
                    IOptions<WasteOrganisationsApiConfiguration>
                >().Value;
                return new CognitoAuthorisationHandler(
                    new CognitoConfig
                    {
                        AccessTokenUrl = config.AccessTokenUrl,
                        ClientId = config.ClientId,
                        ClientSecret = config.ClientSecret,
                    },
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<ILogger<CognitoAuthorisationHandler>>(),
                    sp.GetRequiredService<IMemoryCache>(),
                    "WasteOrganisationsApi_AccessToken"
                );
            })
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<IWasteOrganisationsService>>()!,
                        wasteOrganisationsApiConfig.RetryAttempts,
                        wasteOrganisationsApiConfig.RetryDelaySeconds,
                        Common.Constants.HttpClientNames.WasteOrganisations
                    )
            );

        RrepwApiConfiguration rrepwApiConfig = new();
        configuration.GetSection(RrepwApiConfiguration.SectionName).Bind(rrepwApiConfig);
        services
            .AddHttpClient(Common.Constants.HttpClientNames.Rrepw)
            .AddPolicyHandler(
                (services, request) =>
                    GetRetryPolicy(
                        services.GetService<ILogger<IRrepwService>>()!,
                        rrepwApiConfig.RetryAttempts,
                        rrepwApiConfig.RetryDelaySeconds,
                        Common.Constants.HttpClientNames.Rrepw
                    )
            );

        return services;
    }

    public static IServiceCollection ConfigureOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ServiceBusConfiguration>(
            configuration.GetSection(ServiceBusConfiguration.SectionName)
        );
        services.Configure<NpwdIntegrationConfiguration>(
            configuration.GetSection(NpwdIntegrationConfiguration.SectionName)
        );
        services.Configure<WasteOrganisationsApiConfiguration>(
            configuration.GetSection(WasteOrganisationsApiConfiguration.SectionName)
        );
        services.Configure<UpdateWasteOrganisationsConfiguration>(
            configuration.GetSection(UpdateWasteOrganisationsConfiguration.SectionName)
        );
        services.Configure<FetchRrepwIssuedPrnsConfiguration>(
            configuration.GetSection(FetchRrepwIssuedPrnsConfiguration.SectionName)
        );
        services.Configure<UpdateRrepwPrnsConfiguration>(
            configuration.GetSection(UpdateRrepwPrnsConfiguration.SectionName)
        );
        services.Configure<RrepwApiConfiguration>(
            configuration.GetSection(RrepwApiConfiguration.SectionName)
        );
        services.Configure<Service>(configuration.GetSection("Service"));
        services.Configure<MessagingConfig>(configuration.GetSection("MessagingConfig"));
        services.Configure<FeatureManagementConfiguration>(
            configuration.GetSection(FeatureManagementConfiguration.SectionName)
        );
        services.Configure<AppInsightsConfig>(
            configuration.GetSection(AppInsightsConfig.SectionName)
        );
        return services;
    }

    public static IServiceCollection AddServiceBus(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var isRunningLocally = configuration.GetValue<bool?>("IsRunningLocally");
        if (isRunningLocally is true)
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddClient<ServiceBusClient, ServiceBusClientOptions>(options =>
                {
                    var sp = services.BuildServiceProvider();
                    var serviceBusConfig = sp.GetRequiredService<
                        IOptions<ServiceBusConfiguration>
                    >().Value;
                    options.TransportType = Enum.TryParse<ServiceBusTransportType>(
                        serviceBusConfig.TransportType,
                        out var transportType
                    )
                        ? transportType
                        : ServiceBusTransportType.AmqpWebSockets;
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
                    var serviceBusConfig = sp.GetRequiredService<
                        IOptions<ServiceBusConfiguration>
                    >().Value;
                    return new(
                        serviceBusConfig.FullyQualifiedNamespace,
                        new DefaultAzureCredential(),
                        options
                    );
                });
            });
        }
        return services;
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        ILogger logger,
        int retryCount,
        double sleepDuration,
        string requestType
    )
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount,
                (retryAttempt, res, ctx) =>
                {
                    if (res.Result != null)
                    {
                        var retryAfterHeader = res.Result.Headers.FirstOrDefault(h =>
                            h.Key.Equals("retry-after", StringComparison.InvariantCultureIgnoreCase)
                        );
                        int retryAfter = 0;
                        if (
                            res.Result.StatusCode == HttpStatusCode.TooManyRequests
                            && retryAfterHeader.Value != null
                            && retryAfterHeader.Value.Any()
                        )
                        {
                            retryAfter = int.Parse(retryAfterHeader.Value.First());
                            return TimeSpan.FromSeconds(retryAfter);
                        }
                    }
                    return TimeSpan.FromSeconds(sleepDuration);
                },
                async (response, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        "Retry attempt {retryAttempt} for service {requestType} with delay {delay} seconds as previous request was responded with {StatusCode}",
                        retryAttempt,
                        requestType,
                        timespan.TotalSeconds,
                        response.Result?.StatusCode
                    );
                    await Task.CompletedTask;
                }
            );
    }

    public static IServiceCollection AddCustomApplicationInsights(this IServiceCollection services)
    {
        // Add AI worker service with custom options
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable(
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            );
        });

        // Configure Functions-specific AI settings
        services.ConfigureFunctionsApplicationInsights();

        // Customize logging rules for Application Insights
        services.Configure<LoggerFilterOptions>(options =>
        {
            const string aiProvider =
                "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider";

            // Remove existing default rule for AI provider, if any
            var defaultRule = options.Rules.FirstOrDefault(r => r.ProviderName == aiProvider);
            if (defaultRule != null)
            {
                options.Rules.Remove(defaultRule);
            }

            // Add a new rule to log Information level and above for all categories
            options.Rules.Add(
                new LoggerFilterRule(
                    providerName: aiProvider,
                    categoryName: null,
                    logLevel: LogLevel.Information,
                    filter: null
                )
            );
        });

        return services;
    }
}
