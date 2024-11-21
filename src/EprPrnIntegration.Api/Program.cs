using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;

namespace EprPrnIntegration.Api;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static void Main(string[] args)
    {
        var hostBuilderConfiguration = new HostBuilderConfiguration();
        var host = hostBuilderConfiguration.BuildHost();
        host.Run();
    }
}