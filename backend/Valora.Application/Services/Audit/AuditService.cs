using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;
public sealed class AuditService(IAuditRepository repo){ public Task LogAsync(AuditEntry e)=>repo.AddAsync(e); }
