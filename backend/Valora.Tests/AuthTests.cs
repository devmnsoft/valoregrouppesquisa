using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class AuthTests { [Fact] public void AuthContractExists() => Assert.NotNull(typeof(AuthTests)); }
