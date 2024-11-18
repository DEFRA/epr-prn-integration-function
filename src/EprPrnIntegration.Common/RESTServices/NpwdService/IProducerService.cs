using EprPrnIntegration.Common.Models.Npwd;

namespace EprPrnIntegration.Common.RESTServices.NpwdService;

public interface IProducerService
{
    Task<HttpResponseMessage> UpdateProducerList(List<Producer> updatedProducers);
}