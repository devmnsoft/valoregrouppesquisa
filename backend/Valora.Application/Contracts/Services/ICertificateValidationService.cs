using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ICertificateValidationService { Task<CertificateValidationDto?> ValidateAsync(string code,string? ipHash,string? userAgent); }
