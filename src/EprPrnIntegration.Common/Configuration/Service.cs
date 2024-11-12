namespace EprPrnIntegration.Common.Configuration
{
    public class Service
    {
        public string? Url { get; set; }
        public string? EndPointName { get; set; }
        public string? BearerToken { get; set; }
        public string? HttpClientName { get; set; }
        public int? Retries { get; set; }
    }
}
