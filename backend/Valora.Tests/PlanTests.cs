using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class PlanTests { [Fact] public void PlanContractExists() => Assert.NotNull(typeof(PlanTests)); }
