using Valora.Application.Communication;
using Valora.Application.Services;
using Valora.Application.Access;
using Valora.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Valora.Api.Operations;

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
        services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
        services.AddJwtAuthentication(configuration);
        services.AddAuthorization(options =>
        {
            foreach (var permission in ValoraPermissions.All)
                options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));
        });
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IConfigurationValidationService, ConfigurationValidationService>();
        return services;
    }
}
