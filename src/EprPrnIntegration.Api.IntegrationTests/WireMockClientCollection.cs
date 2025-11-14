using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

[CollectionDefinition("WireMock")]
public class WireMockClientCollection : ICollectionFixture<WireMockFixture>;