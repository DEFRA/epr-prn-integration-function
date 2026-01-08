using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public sealed class MessagingServices : IMessagingServices
{
    public IServiceBusProvider ServiceBusProvider { get; }
    public IEmailService EmailService { get; }

    public MessagingServices(IServiceBusProvider serviceBusProvider, IEmailService emailService)
    {
        ServiceBusProvider =
            serviceBusProvider ?? throw new ArgumentNullException(nameof(serviceBusProvider));
        EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }
}
