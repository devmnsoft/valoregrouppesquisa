using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Valora.Api.Configuration;

public static class JwtConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var signingKey = jwt["SigningKey"];
        // Espaços não aumentam artificialmente a entropia mínima exigida para a chave.
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Trim().Length < 32)
            throw new InvalidOperationException(
                "Jwt:SigningKey deve possuir pelo menos 32 caracteres. Configure Jwt:SigningKey em appsettings.Development.json, user-secrets ou variável de ambiente.");

        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    // Evita que uma configuração incorreta aceite tokens expirados por tempo excessivo.
                    ClockSkew = TimeSpan.FromSeconds(Math.Clamp(jwt.GetValue("ClockSkewSeconds", 30), 0, 300))
                };
            });
        return services;
    }
}
