using Azure.Identity;
using EprPrnIntegration.Common;
using EprPrnIntegration.Common.Middleware;
using EprPrnIntegration.Common.Service;
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

        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddTransient<NpwdOAuthMiddleware>();
        services.AddHttpClient(Constants.HttpClientNames.Npwd).AddHttpMessageHandler<NpwdOAuthMiddleware>();

        var keyVaultUrl = Environment.GetEnvironmentVariable(Constants.ConfigSettingKeys.KeyVaultUrl) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
            throw new ConfigurationErrorsException(Constants.ConfigSettingKeys.KeyVaultUrl);

        var appDirectory = AppContext.BaseDirectory;

        _ = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile(Path.Combine(appDirectory, "settings.json"), optional: true, reloadOnChange: true)
            .AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential())
            .Build();

    })
    .Build();

host.Run();
