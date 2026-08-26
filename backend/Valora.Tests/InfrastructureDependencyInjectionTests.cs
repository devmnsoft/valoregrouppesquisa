using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.FormalDeliverables;
using Valora.Application.ValoraAi;
using Valora.Infrastructure.DependencyInjection;
using Valora.Infrastructure.FormalDeliverables;
using Valora.Infrastructure.Repositories;
using Valora.Application.Subscriptions;
using Valora.Infrastructure.Subscriptions;

namespace Valora.Tests;

public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersAiAndFormalDeliverableRepositories()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructureServices(configuration);

        AssertScoped<IValoraAiRunRepository, ValoraAiRunRepository>(services);
        AssertScoped<IDiagnosisDocumentSnapshotProvider, DiagnosisDocumentSnapshotProvider>(services);
        AssertScoped<IShareLinkRepository, ShareLinkRepository>(services);
        AssertScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>(services);
        AssertScoped<IOrganizationSubscriptionRepository, OrganizationSubscriptionRepository>(services);
        AssertScoped<IUsageCounterRepository, UsageCounterRepository>(services);
        AssertScoped<IUpgradeRequestRepository, UpgradeRequestRepository>(services);
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(TService));
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
