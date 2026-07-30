using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class CertificateValidationService(ICertificateOperationalRepository repo,IAuditRepository audit):ICertificateValidationService{ public async Task<CertificateValidationDto?> ValidateAsync(string code,string? ipHash,string? userAgent){ var r=await repo.ValidateAsync(code,ipHash,userAgent); await audit.AddAsync(new AuditEntry(null,null,"certificate.validated","certificate",code,"Certificado validado","{}")); return r; }}
