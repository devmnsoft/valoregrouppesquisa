using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Certificates;
using Valora.Application.Communication;
using Valora.Application.FreeDiagnostics;
using Valora.Application.Contracts;
using Valora.Application.Results;
using Valora.Application.Services;
using Valora.Application.Contracts;

namespace Valora.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<ValoraInsightCalculator>();
        services.AddSingleton<ValoraInsightDevolutivaService>();
        services.AddScoped<CertificateService>();
        services.AddScoped<ICertificateService>(sp => sp.GetRequiredService<CertificateService>());
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

        services.AddScoped<Valora.Application.Contracts.ILegacyDataNormalizer, LegacyDataNormalizer>();
        services.AddScoped<Valora.Application.Contracts.ILegacyMappingService, LegacyMappingService>();
        services.AddScoped<IFirestoreExportReader, FirestoreExportReader>();
        services.AddScoped<ILocalStorageExportReader, LocalStorageExportReader>();
        services.AddScoped<IManualJsonReader, ManualJsonReader>();
        services.AddScoped<ILegacySourceReader>(sp => sp.GetRequiredService<IFirestoreExportReader>());
        services.AddScoped<ILegacySourceReader>(sp => sp.GetRequiredService<ILocalStorageExportReader>());
        services.AddScoped<ILegacySourceReader>(sp => sp.GetRequiredService<IManualJsonReader>());
        services.AddScoped<IMigrationDryRunService, MigrationDryRunService>();
        services.AddScoped<Valora.Application.Contracts.ILegacyImportService>(sp => (MigrationDryRunService)sp.GetRequiredService<IMigrationDryRunService>());
        services.AddScoped<IMigrationApplyService, MigrationApplyService>();
        services.AddScoped<IMigrationReconciliationService, MigrationReconciliationService>();
        services.AddScoped<IMigrationRollbackService, MigrationRollbackService>();
        services.AddScoped<ICutoverReadinessService, CutoverReadinessService>();
        services.AddScoped<IMigrationReportService, MigrationReportService>();
        services.AddSingleton<IResultTokenService, ResultTokenService>();
        return services;
    }
}
