using Azure.Identity;
using EprPrnIntegration.Common.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;

var host = new HostBuilder()
    //.ConfigureFunctionsWebApplication()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddHttpClient(EprPrnIntegration.Common.Constants.HttpClientNames.Npwd)
            .AddHttpMessageHandler<NpwdOAuthMiddleware>();

        var keyVaultUrl = Environment.GetEnvironmentVariable("AzureKeyVaultUrl") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
            throw new ConfigurationErrorsException("AzureKeyVaultUrl");

        var appDirectory = AppContext.BaseDirectory;

        var config = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile(Path.Combine(appDirectory, "settings.json"), optional: true, reloadOnChange: true)
            .AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential())
            .Build();

        //services.AddSingleton<IConfiguration>(config);
    })
    .Build();

host.Run();
