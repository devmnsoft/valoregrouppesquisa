using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IJwtTokenService
{
    string CreateToken(Guid userId, Guid organizationId, Guid sessionId, string email, string role, string locale);
}
