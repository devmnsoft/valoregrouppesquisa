using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ICertificateOperationalService { Task<CertificateDto> GenerateAsync(Guid organizationId,Guid responseId); Task<IReadOnlyList<CertificateDto>> ListAsync(Guid organizationId); Task<CertificateDto?> GetAsync(Guid organizationId,Guid id); Task<string?> DownloadHtmlAsync(Guid organizationId,Guid id); Task RevokeAsync(Guid organizationId,Guid id); }
