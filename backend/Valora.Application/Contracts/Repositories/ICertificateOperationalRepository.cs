using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ICertificateOperationalRepository { Task<CertificateDto?> GetAsync(Guid organizationId,Guid id); Task<CertificateDto?> GetByResponseAsync(Guid organizationId,Guid responseId); Task<IReadOnlyList<CertificateDto>> ListAsync(Guid organizationId); Task<CertificateDto> CreateAsync(Guid organizationId,Guid? surveyId,Guid responseId,string? participantName,string? maskedEmail,string? companyName,decimal? totalScore,string? level,string validationCode,string validationUrl,string payloadJson); Task RevokeAsync(Guid organizationId,Guid id); Task<CertificateValidationDto?> ValidateAsync(string code,string? ipHash,string? userAgent); }
