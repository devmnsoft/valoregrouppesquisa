using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Contracts;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Email;
using Valora.Infrastructure.Repositories;
using Valora.Infrastructure.Security;

namespace Valora.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDbConnectionFactory, PostgresConnectionFactory>();
        services.AddScoped<MigrationRunner>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IResultRepository, ResultRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IMigrationRepository, MigrationRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IFreeDiagnosticsRepository, FreeDiagnosticsRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        return services;
    }

    public static IServiceCollection AddValoraInfrastructure(this IServiceCollection services) => services;
}
