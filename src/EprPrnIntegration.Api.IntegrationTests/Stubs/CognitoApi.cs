using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public class CognitoApi(WireMockContext wireMock)
{
    public async Task SetupOAuthToken()
    {
        var accessToken = $"test-bearer-token-{Guid.NewGuid()}";

        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(builder =>
            builder.WithRequest(request => request
                    .UsingPost()
                    .WithPath("/cognito/oauth/token")
                    .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                )
                .WithResponse(response => response
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new
                    {
                        access_token = accessToken,
                        token_type = "Bearer",
                        expires_in = 3600
                    }))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }
    
    public async Task<IList<LogEntryModel>> GetTokenRequests()
    {
        var requestsModel = new RequestModel { Methods = ["POST"], Path = "/cognito/oauth/*" };
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestsModel);
    }
}
