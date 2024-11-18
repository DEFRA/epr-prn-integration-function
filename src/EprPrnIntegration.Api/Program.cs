using Azure.Identity;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Middleware;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.NpwdService;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient();
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IProducerService, ProducerService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddTransient<NpwdOAuthMiddleware>();
        services.AddHttpClient(EprPrnIntegration.Common.Constants.HttpClientNames.Npwd).AddHttpMessageHandler<NpwdOAuthMiddleware>();

        var keyVaultUrl = Environment.GetEnvironmentVariable(EprPrnIntegration.Common.Constants.ConfigSettingKeys.KeyVaultUrl) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
            throw new ConfigurationErrorsException(EprPrnIntegration.Common.Constants.ConfigSettingKeys.KeyVaultUrl);

        var appDirectory = AppContext.BaseDirectory;

        var config = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile(Path.Combine(appDirectory, "settings.json"), optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine(appDirectory, "local.settings.json"), optional: true, reloadOnChange: true)
            .AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential())
            .Build();

        services.Configure<Service>(config.GetSection("Service"));
    })
    .Build();

host.Run();
