using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Configuration;

[assembly: FunctionsStartup(typeof(EprPrnIntegration.Api.Startup))]
namespace EprPrnIntegration.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder) 
        {
            var keyVaultUrl = Environment.GetEnvironmentVariable("AzureAD:AzureKeyVaultUrl") ?? string.Empty;

            if(string.IsNullOrWhiteSpace(keyVaultUrl))
                throw new ConfigurationErrorsException("AzureAD:AzureKeyVaultUrl");

            var appDirectory = AppContext.BaseDirectory;

            var config = new ConfigurationBuilder()
                .SetBasePath(appDirectory)
                .AddJsonFile(Path.Combine(appDirectory, "settings.json"), optional: true, reloadOnChange: true)
                .AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential())
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
        }
    }
}
