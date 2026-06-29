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
        services.AddScoped<ResultEmailService>();
        services.AddScoped<EmailQueueProcessor>();
        services.AddScoped<AuthService>();
        services.AddScoped<AuditService>();
        services.AddScoped<PlanEntitlementService>();
        services.AddScoped<IPlanEntitlementService>(sp => sp.GetRequiredService<PlanEntitlementService>());
        services.AddScoped<PublicAnswerNormalizer>();
        services.AddScoped<PublicAnswerScorer>();
        services.AddScoped<PublicSurveyValidator>();
        services.AddScoped<PublicSurveyAssembler>();
        services.AddScoped<PublicResponseTransactionService>();
        services.AddScoped<PublicSurveySubmitter>();
        services.AddScoped<IPublicSurveyService, PublicSurveyService>();
        services.AddScoped<PublicResultValidator>();
        services.AddScoped<PublicResultAssembler>();
        services.AddScoped<IPublicResultService, PublicResultService>();
        services.AddSingleton<IResultTokenService, ResultTokenService>();
        return services;
    }
}
