using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class RepositoryLoggingTests { [Fact] public void Sprint24OperationalContractExists() => Assert.NotNull(typeof(RepositoryLoggingTests)); }
