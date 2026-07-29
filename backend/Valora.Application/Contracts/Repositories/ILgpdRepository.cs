using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ILgpdRepository { Task<LgpdConsentDto> AddConsentAsync(RegisterLgpdConsentRequest request,string emailHash,string? ipHash,string? userAgent); Task<IReadOnlyList<LgpdConsentDto>> ListConsentsAsync(Guid organizationId); Task<PrivacyRequestDto> CreateRequestAsync(CreatePrivacyRequestRequest request,string emailHash,string emailMasked); Task<PrivacyRequestDto?> GetRequestAsync(string protocol); Task<PrivacyRequestDto?> GetRequestByIdAsync(Guid id); Task<IReadOnlyList<PrivacyRequestDto>> ListRequestsAsync(Guid? organizationId); Task UpdateStatusAsync(Guid organizationId,Guid id,string status,Guid? handledBy); Task CompleteAsync(Guid organizationId,Guid id,string resultJson,Guid? handledBy); }
