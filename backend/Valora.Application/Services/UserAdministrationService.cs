using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Application.Access;
namespace Valora.Application.Services;

public sealed class UserAdministrationService(IUserAdministrationRepository repository) : IUserAdministrationService {
    public async Task<PagedResponse<UserAdministrationResponse>> ListAsync(Guid org, UserListQuery q, CancellationToken ct = default) { Tenant(org); var page = Math.Max(1, q.Page); var size = Math.Clamp(q.PageSize, 1, 100); var query = q with { Page = page, PageSize = size }; var (items, total) = await repository.ListAsync(org, query, ct); return new(items, page, size, total, (int)Math.Ceiling(total / (double)size)); }
    public async Task<UserAdministrationResponse> GetAsync(Guid org, Guid id, CancellationToken ct = default) => await repository.GetAsync(Tenant(org), id, ct) ?? throw new NotFoundAppException("Usuário não encontrado.");
    public async Task<UserAdministrationResponse> CreateAdministrativeAsync(Guid org, Guid actor, bool actorIsGlobalAdministrator, CreateAdministrativeUserRequest r, CancellationToken ct = default) {
        Tenant(org);
        if (actor == Guid.Empty) throw new ForbiddenAppException("Ator autenticado é obrigatório.");
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ValidationAppException("Nome obrigatório.");
        if (string.IsNullOrWhiteSpace(r.Email)) throw new ValidationAppException("E-mail obrigatório.");
        string roleCode;
        try { roleCode = RoleAssignmentPolicy.NormalizeRoleCode(r.RoleCode); }
        catch (ArgumentException) { throw new ValidationAppException("Papel obrigatório."); }
        if (!RoleAssignmentPolicy.CanAssignRole(roleCode, actorIsGlobalAdministrator))
            throw new ForbiddenAppException("Papel indisponível para atribuição neste contexto.");
        return await repository.CreateAdministrativeAsync(org, actor, r with { Name = r.Name.Trim(), Email = r.Email.Trim().ToLowerInvariant(), RoleCode = roleCode }, ct)
            ?? throw new ValidationAppException("Papel indisponível nesta organização.");
    }
    public async Task<UserAdministrationResponse> UpdateAsync(Guid org, Guid id, UpdateUserRequest r, CancellationToken ct = default) { if (string.IsNullOrWhiteSpace(r.Name)) throw new ValidationAppException("Nome obrigatório."); if (!await repository.UpdateAsync(Tenant(org), id, r, ct)) throw new NotFoundAppException("Usuário não encontrado."); return await GetAsync(org, id, ct); }
    public async Task<UserAdministrationResponse> UpdateAdministrativeAsync(Guid org, Guid actor, bool actorIsGlobalAdministrator, Guid id, UpdateAdministrativeUserRequest r, CancellationToken ct = default) {
        Tenant(org);
        if (actor == Guid.Empty) throw new ForbiddenAppException("Ator autenticado é obrigatório.");
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ValidationAppException("Nome obrigatório.");
        if (r.Status is not ("active" or "inactive" or "suspended" or "locked")) throw new ValidationAppException("Status inválido.");
        IReadOnlyList<string> roles;
        try { roles = RoleAssignmentPolicy.NormalizeRoleCodes(r.RoleCodes ?? []); }
        catch (ArgumentException) { throw new ValidationAppException("Informe ao menos um papel válido."); }
        if (roles.Any(role => !RoleAssignmentPolicy.CanAssignRole(role, actorIsGlobalAdministrator)))
            throw new ForbiddenAppException("Papel reservado à operação global autorizada.");
        return await repository.UpdateAdministrativeAsync(org, actor, id, r with { RoleCodes = roles }, ct) ?? throw new NotFoundAppException("Usuário não encontrado.");
    }
    public async Task UpdateStatusAsync(Guid org, Guid actor, Guid id, UpdateUserStatusRequest r, CancellationToken ct = default) { Tenant(org); if (r.Status is not ("active" or "inactive" or "suspended" or "locked")) throw new ValidationAppException("Status inválido."); if (!await repository.UpdateStatusAsync(org, id, r.Status, ct)) throw new NotFoundAppException("Usuário não encontrado."); }
    public async Task SetRolesAsync(Guid org, Guid actor, Guid id, UpdateUserRolesRequest r, CancellationToken ct = default) {
        Tenant(org);
        IReadOnlyList<string> roles;
        try { roles = (r.RoleCodes ?? []).Select(RoleAssignmentPolicy.NormalizeRoleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }
        catch (ArgumentException) { throw new ValidationAppException("Informe apenas papéis válidos."); }
        if (roles.Any(role => !RoleAssignmentPolicy.CanAssignRole(role, actorIsGlobalAdministrator: false))) throw new ForbiddenAppException("Papel reservado à operação global autorizada.");
        await repository.ReplaceRolesAsync(org, id, roles, ct);
    }
    public async Task SetScopesAsync(Guid org, Guid actor, Guid id, UpdateUserScopesRequest r, CancellationToken ct = default) { Tenant(org); await repository.ReplaceScopesAsync(org, id, r.Scopes, ct); }
    private static Guid Tenant(Guid id) => id == Guid.Empty ? throw new ForbiddenAppException("Tenant obrigatório.") : id;
}
