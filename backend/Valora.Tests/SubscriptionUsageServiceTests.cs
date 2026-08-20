using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Tests;

public sealed class SubscriptionUsageServiceTests
{
    [Fact]
    public async Task Common_organization_is_blocked_with_upgrade_cta_at_limit()
    {
        var service = CreateService(10, 10);
        var result = await service.CheckAsync(Guid.NewGuid(), "responsesPerMonth");
        Assert.False(result.Allowed);
        Assert.Equal("/Organization/Upgrade", result.UpgradeUrl);
        Assert.Contains("upgrade", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Valora_admin_is_never_blocked_by_organization_limit()
    {
        var service = CreateService(10, 10);
        var result = await service.CheckAsync(Guid.NewGuid(), "responsesPerMonth", isValoraAdmin: true);
        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task Current_usage_calculates_percentage()
    {
        var snapshot = await CreateService(8, 10).GetCurrentAsync(Guid.NewGuid());
        Assert.Equal(80, snapshot.Metrics.Single(x => x.Code == "responsesPerMonth").PercentUsed);
    }

    private static SubscriptionUsageService CreateService(int used, int limit) => new(
        new FakeUsageRepository(used), new FakeEntitlements(limit));

    private sealed class FakeUsageRepository(int responses) : IUsageRepository
    {
        public Task<UsageDto> GetMonthlyAsync(Guid organizationId, DateTime month) =>
            Task.FromResult(new UsageDto(0, responses, 0, new() { ["responsesPerMonth"] = responses }));
        public Task RecalculateAsync(Guid organizationId, DateTime month) => Task.CompletedTask;
    }

    private sealed class FakeEntitlements(int limit) : IPlanEntitlementService
    {
        public Task<PlanEntitlements> ResolveAsync(Guid organizationId) => Task.FromResult(new PlanEntitlements(
            "test", new Dictionary<string, int> { ["responsesPerMonth"] = limit }, new Dictionary<string, string>()));
        public Task<LimitCheckResult> CheckLimitAsync(Guid organizationId, string limitKey, int requestedAmount = 1) => throw new NotSupportedException();
        public Task<UsageDto> GetUsageAsync(Guid organizationId) => throw new NotSupportedException();
    }
}
