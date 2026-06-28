using Valora.Application.Communication;

namespace Valora.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddCors(options =>
        {
            options.AddPolicy("ValoraWebCors", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5088", "http://127.0.0.1:5088" };

                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        services.AddSwaggerDocumentation();
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();
        return services;
    }
}
