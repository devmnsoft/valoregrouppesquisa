using Valora.Tests.Support;
using Xunit;

[Trait("Category", "Unit")]
public sealed class CommunicationEndpointContractTests
{
    [Fact]
    public void Sprint46ContractIsDocumented() => Assert.True(File.Exists(RepositoryPaths.RootFile("SPRINT_46_FREE_DIAGNOSTIC_E2E_AUDIT.md")));
}
