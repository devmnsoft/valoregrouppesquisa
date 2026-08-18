using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Security;
public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateToken(Guid userId, Guid organizationId, Guid sessionId, string email, string role, string locale)
    {
        var options = configuration.GetSection("Jwt");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("organizationId", organizationId.ToString()),
            new("sessionId", sessionId.ToString()),
            new("role", role),
            new("locale", locale)
        };
        var signingKey = options["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey não está configurada.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var accessTokenMinutes = int.TryParse(options["AccessTokenMinutes"], out var configuredMinutes)
            ? configuredMinutes : 15;
        var expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes);
        var token = new JwtSecurityToken(options["Issuer"], options["Audience"], claims, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
