using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Contracts;
using Valora.Application.Forms;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Email;
using Valora.Infrastructure.Repositories;
using Valora.Infrastructure.Security;
using Valora.Application.Access;
using Valora.Application.Enterprise;
using Valora.Application.OrganizationalIntelligence;
using Valora.Application.ValoraBot;
using Valora.Application.Methodology;
using Valora.Application.DiagnosticWorkspace;
using Valora.Application.CommercialDelivery;
using Valora.Application.Integrations;
using Valora.Application.DecisionCenter;
using Valora.Application.FormalDeliverables;
using Valora.Application.ValoraAi;
using Valora.Infrastructure.FormalDeliverables;

namespace Valora.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDbConnectionFactory, PostgresConnectionFactory>();
        services.AddScoped<IDbTransactionFactory, DbTransactionFactory>();
        services.AddScoped<MigrationRunner>();
        services.AddScoped<SchemaContractValidator>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ISaasAdministrationRepository, SaasAdministrationRepository>();
        services.AddScoped<IOrganizationStructureRepository, OrganizationStructureRepository>();
        services.AddScoped<IOrganizationBrandingRepository, OrganizationBrandingRepository>();
        services.AddScoped<IUserAdministrationRepository, UserAdministrationRepository>();
        services.AddScoped<Valora.Application.CompanyRegistration.ICompanyRegistrationRepository, CompanyRegistrationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IAccessAdministrationRepository, AccessAdministrationRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFormAdministrationRepository, FormAdministrationRepository>();
        services.AddScoped<IResultRepository, ResultRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IMigrationRepository, MigrationRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IFreeDiagnosticsRepository, FreeDiagnosticsRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUsageRepository, UsageRepository>();
        services.AddScoped<IDashboardMetricsRepository, DashboardMetricsRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ICertificateOperationalRepository, CertificateOperationalRepository>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<ILgpdRepository, LgpdRepository>();
        services.AddScoped<IEmailOperationalRepository, EmailOperationalRepository>();
        services.AddScoped<IMigrationBatchRepository, MigrationBatchRepository>();
        services.AddScoped<IMigrationSourceFileRepository, MigrationSourceFileRepository>();
        services.AddScoped<IMigrationRecordRepository, MigrationRecordRepository>();
        services.AddScoped<IMigrationMappingRepository, MigrationMappingRepository>();
        services.AddScoped<IMigrationConflictRepository, MigrationConflictRepository>();
        services.AddScoped<IMigrationRollbackRepository, MigrationRollbackRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEnterpriseRepository, EnterpriseRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IOrganizationalIntelligenceRepository, OrganizationalIntelligenceRepository>();
        services.AddScoped<IDecisionCenterRepository, DecisionCenterRepository>();
        services.AddScoped<IIntelligencePipelineRepository, IntelligencePipelineRepository>();
        services.AddScoped<IBenchmarkRepository, BenchmarkRepository>();
        services.AddScoped<IIntelligenceProcessingJobRepository, IntelligenceProcessingJobRepository>();
        services.AddScoped<IValoraBotRepository, ValoraBotRepository>();
        services.AddScoped<IMethodologyRepository, MethodologyRepository>();
        services.AddScoped<IMethodologyStudioRepository, MethodologyStudioRepository>();
        services.AddScoped<IDiagnosticWorkspaceRepository, DiagnosticWorkspaceRepository>();
        services.AddScoped<IDiagnosticCampaignRepository, DiagnosticCampaignRepository>();
        services.AddScoped<IAssistedOperationsRepository, AssistedOperationsRepository>();
        services.AddScoped<IPublicCommercialRepository, PublicCommercialRepository>();
        services.AddScoped<IValoraAiRunRepository, ValoraAiRunRepository>();
        services.AddScoped<IDiagnosisDocumentSnapshotProvider, DiagnosisDocumentSnapshotProvider>();
        services.AddScoped<IShareLinkRepository, ShareLinkRepository>();
        services.AddScoped<IDocumentAccessPolicy, DocumentAccessPolicy>();
        services.AddScoped<IDocumentStore, DocumentStore>();
        services.AddScoped<IExportAuditService, ExportAuditService>();
        return services;
    }

    public static IServiceCollection AddValoraInfrastructure(this IServiceCollection services) => services;
}
