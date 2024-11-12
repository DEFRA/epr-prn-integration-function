using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ServicesConfiguration
    {
        public static string SectionName => "Services";

        public Service PaymentService { get; set; } = new Service();
        public Service GovPayService { get; set; } = new Service();
        public Service ProducerFeesService { get; set; } = new Service();
        public Service ComplianceSchemeFeesService { get; set; } = new Service();
    }
}
