using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Certificates;
using Valora.Application.Communication;
using Valora.Application.Contracts;
using Valora.Application.Results;
using Valora.Application.Services;

namespace Valora.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<ValoraInsightCalculator>();
        services.AddSingleton<ValoraInsightDevolutivaService>();
        services.AddSingleton<CertificateService>();
        services.AddSingleton<EmailService>();
        services.AddSingleton<EmailJobService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AuditService>();
        services.AddScoped<PlanEntitlementService>();
        return services;
    }
}
