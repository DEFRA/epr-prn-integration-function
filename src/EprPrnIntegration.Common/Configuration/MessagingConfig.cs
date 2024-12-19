using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Configuration;

[ExcludeFromCodeCoverage]
public class MessagingConfig
{
    public const string SectionName = "MessagingConfig";

    public string? PrnTemplateId { get; set; }
    public string? PERNTemplateId { get; set; }
    public string? NpwdEmailTemplateId { get; set; }
    public string? NpwdEmail { get; set; }
    public string? PernTemplateId { get; set; }
    public string? ApiKey { get; set; }
}