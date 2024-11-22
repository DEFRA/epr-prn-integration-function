using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ServiceBusConfiguration
    {
        public const string SectionName = "ServiceBus";
        public string FullyQualifiedNamespace { get; set; } = null!;
        public string FetchPrnQueueName { get; set; } = null!;
        public string ErrorPrnQueue { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
    }
}
