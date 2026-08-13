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
        services.AddScoped<IOrganizationalIntelligenceRepository, OrganizationalIntelligenceRepository>();
        services.AddScoped<IValoraBotRepository, ValoraBotRepository>();
        services.AddScoped<IMethodologyRepository, MethodologyRepository>();
        return services;
    }

    public static IServiceCollection AddValoraInfrastructure(this IServiceCollection services) => services;
}
