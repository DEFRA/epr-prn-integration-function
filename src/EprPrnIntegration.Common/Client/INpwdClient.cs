using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.Client;

public interface INpwdClient
{
    Task<HttpResponseMessage> Patch<T>(T updatedProducers, string updatePath);
    Task<List<NpwdPrn>> GetIssuedPrns(string filter);
}
