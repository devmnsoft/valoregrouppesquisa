using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Certificates;
using Valora.Application.Communication;
using Valora.Application.FreeDiagnostics;
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
        services.AddScoped<FreeDiagnosticsAppService>();
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
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDashboardMetricsService, DashboardMetricsService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<ReportBuilderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICertificateOperationalService, CertificateOperationalService>();
        services.AddScoped<ICertificateValidationService, CertificateValidationService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ILgpdConsentService, LgpdConsentService>();
        services.AddScoped<IPrivacyRequestService, PrivacyRequestService>();
        services.AddScoped<IEmailTemplateService, OperationalEmailTemplateService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();
        services.AddScoped<IEmailStatusService, EmailStatusService>();
        services.AddSingleton<IResultTokenService, ResultTokenService>();
        return services;
    }
}
