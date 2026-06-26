using Valora.Application.Communication;

namespace Valora.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddSwaggerDocumentation();
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();
        return services;
    }
}
