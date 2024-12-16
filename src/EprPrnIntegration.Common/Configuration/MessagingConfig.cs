using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class MessagingConfig
{
    public string? PrnTemplateId { get; set; }
    public string? PernTemplateId { get; set; }
    public string? ApiKey { get; set; }
}