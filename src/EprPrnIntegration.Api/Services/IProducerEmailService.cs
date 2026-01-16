using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Services;

public interface IProducerEmailService
{
    Task SendEmailToProducersAsync(
        SavePrnDetailsRequest request,
        WoApiOrganisation? woOrganisation,
        ILogger logger,
        IOrganisationService organisationService,
        IEmailService emailService
    );
}
