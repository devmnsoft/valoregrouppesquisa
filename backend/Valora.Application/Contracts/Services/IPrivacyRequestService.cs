using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IPrivacyRequestService { Task<PrivacyRequestDto> CreatePublicAsync(CreatePrivacyRequestRequest request); Task<PrivacyRequestDto?> GetPublicAsync(string protocol); Task<IReadOnlyList<PrivacyRequestDto>> ListAsync(Guid organizationId); Task UpdateStatusAsync(Guid organizationId,Guid id,string status,Guid? handledBy); Task CompleteAsync(Guid organizationId,Guid id,string resultJson,Guid? handledBy); }
