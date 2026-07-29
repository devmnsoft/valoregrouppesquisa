using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ILgpdConsentService { Task<LgpdConsentDto> RegisterAsync(RegisterLgpdConsentRequest request,string? ipHash,string? userAgent); Task<IReadOnlyList<LgpdConsentDto>> ListAsync(Guid organizationId); }
