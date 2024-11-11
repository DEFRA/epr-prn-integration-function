using Azure.Identity;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.BackendAccountService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using Microsoft.AspNetCore.Http;
using EprPrnIntegration.Common.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient();
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        var keyVaultUrl = Environment.GetEnvironmentVariable("AzureKeyVaultUrl") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
            throw new ConfigurationErrorsException("AzureKeyVaultUrl");

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
