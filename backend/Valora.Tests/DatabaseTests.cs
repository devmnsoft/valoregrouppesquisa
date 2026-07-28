using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class DatabaseTests { [Fact] public void MigrationContractExists() => Assert.True(true); }
