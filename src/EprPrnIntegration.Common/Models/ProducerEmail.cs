using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class ProducerEmail
    {
        public required string EmailAddress { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string NameOfExporterReprocessor { get; set; }
        public required string NameOfProducerComplianceScheme { get; set; }
        public required string PrnNumber { get; set; }
        public required string Material { get; set; }
        public decimal Tonnage { get; set; }
        public bool IsExporter { get; set; }
        public required string ProducerAgency { get; set; }
    }
}
