using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IJwtTokenService { string CreateToken(Guid userId, Guid? organizationId, string email, string role); }
