using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Api.Services;

public interface IProducerEmailService
{
    Task SendEmailToProducersAsync(
        SavePrnDetailsRequest request,
        WoApiOrganisation? woOrganisation
    );
}
