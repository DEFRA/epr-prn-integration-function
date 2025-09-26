namespace EprPrnIntegration.Common.Service;

public interface IMessagingServices
{
    IServiceBusProvider ServiceBusProvider { get; }
    IEmailService EmailService { get; }
}