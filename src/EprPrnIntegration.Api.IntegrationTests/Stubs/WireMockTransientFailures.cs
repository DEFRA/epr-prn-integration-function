using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Client.Extensions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Stubs;

public static class WireMockTransientFailures
{
    public static async Task WithEndpointRecoveringFromTransientFailures(
        this WireMockContext wireMock,
        Func<RequestModelBuilder, RequestModelBuilder> configureRequest,
        Func<ResponseModelBuilder, ResponseModelBuilder> configureSuccessResponse,
        Func<ResponseModelBuilder, ResponseModelBuilder> configureFailureResponse,
        int failureCount = 2
    )
    {
        var scenarioName = Guid.NewGuid().ToString();

        // Set up failure mappings: each failure transitions to the next state
        for (var i = 0; i < failureCount; i++)
        {
            var currentState = i == 0 ? null : $"Attempt{i}";
            var nextState = $"Attempt{i + 1}";

            var failureMapping = wireMock.WireMockAdminApi.GetMappingBuilder();
            failureMapping.Given(builder =>
            {
                var mappingBuilder = builder
                    .WithRequest(r => configureRequest(r))
                    .WithResponse(r => configureFailureResponse(r))
                    .WithScenario(scenarioName)
                    .WithSetStateTo(nextState);

                // Only set WhenStateIs for subsequent failures (not the first one)
                if (currentState != null)
                {
                    mappingBuilder.WithWhenStateIs(currentState);
                }
            });
            var failureMappingStatus = await failureMapping.BuildAndPostAsync();
            Assert.NotNull(failureMappingStatus.Guid);
        }

        // Final mapping: return success when in the last failure state
        var finalState = $"Attempt{failureCount}";
        var successMapping = wireMock.WireMockAdminApi.GetMappingBuilder();
        successMapping.Given(builder =>
            builder
                .WithRequest(r => configureRequest(r))
                .WithResponse(r => configureSuccessResponse(r))
                .WithScenario(scenarioName)
                .WithWhenStateIs(finalState)
        );
        var successMappingStatus = await successMapping.BuildAndPostAsync();
        Assert.NotNull(successMappingStatus.Guid);
    }
}
