namespace EprPrnIntegration.Common.Service;

public interface IEmailService
{
    void SendEmailToNpwd(string npwdEmailAddress, string errorMessage);
}
