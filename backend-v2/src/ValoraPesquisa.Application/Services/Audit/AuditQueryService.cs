using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs;
namespace ValoraPesquisa.Application.Services.Audit;
public sealed class AuditQueryService(IAuditService audit){ public Task<IReadOnlyList<AuditEventDto>> ExecuteAsync(CurrentUser user)=>audit.ListAsync(user); }
