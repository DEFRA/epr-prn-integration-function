using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class MessagingConfig
{
    public string? PrnTemplateId { get; set; }
    public string? PERNTemplateId { get; set; }
    public string? NPWDTemplateId { get; set; }
    public string? NPWDEmail { get; set; }
    public string? ApiKey { get; set; }
}