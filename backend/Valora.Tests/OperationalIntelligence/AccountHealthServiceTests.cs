using Valora.Application.OperationalIntelligence;

namespace Valora.Tests.OperationalIntelligence;

public sealed class AccountHealthServiceTests
{
    [Fact]
    public void Evaluate_EmptyAccount_IsCriticalAndSuggestsConcreteRoutes()
    {
        var result = new AccountHealthService().Evaluate(new(false, false, 0, 0, 0, 0, true, 0, 0, 0));
        Assert.Equal(AccountHealthStatus.Critical, result.Status);
        Assert.Equal(20, result.Score);
        Assert.Contains(result.NextActions, action => action.Code == "unit" && action.Route == "/Organization#org-structure");
        Assert.All(result.NextActions, action => Assert.StartsWith("/", action.Route));
    }

    [Fact]
    public void Evaluate_CompleteOperatingCycle_IsExcellent()
    {
        var result = new AccountHealthService().Evaluate(new(true, true, 2, 3, 1, 20, true, 1, 0, 2));
        Assert.Equal(AccountHealthStatus.Excellent, result.Status);
        Assert.Equal(100, result.Score);
        Assert.Empty(result.NextActions);
    }

    [Theory]
    [InlineData("Gratuito", ComparisonScope.Department, false)]
    [InlineData("Profissional", ComparisonScope.Department, true)]
    [InlineData("Profissional", ComparisonScope.Unit, false)]
    [InlineData("Corporativo", ComparisonScope.Unit, true)]
    [InlineData("Enterprise", ComparisonScope.MultiOrganization, true)]
    public void ComparisonPolicy_RespectsPlanMatrix(string plan, ComparisonScope scope, bool expected) =>
        Assert.Equal(expected, new ComparisonEntitlementPolicy().CanAccess(plan, scope));
}
