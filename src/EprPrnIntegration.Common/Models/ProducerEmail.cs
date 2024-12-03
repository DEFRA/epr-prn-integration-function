namespace EprPrnIntegration.Api.Models
{
    public class ProducerEmail
    {
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NameOfExporterReprocessor { get; set; }
        public string NameOfProducerComplianceScheme { get; set; }
        public string PrnNumber { get; set; }
        public string Material { get; set; }
        public decimal Tonnage { get; set; }
        public bool IsPrn { get; set; }
    }
}
