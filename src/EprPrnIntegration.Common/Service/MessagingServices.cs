using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public sealed class MessagingServices
{
    public IServiceBusProvider ServiceBusProvider { get; }
    public IEmailService EmailService { get; }

    public MessagingServices(
        IServiceBusProvider serviceBusProvider,
        IEmailService emailService)
    {
        ServiceBusProvider = serviceBusProvider;
        EmailService = emailService;
    }
}

