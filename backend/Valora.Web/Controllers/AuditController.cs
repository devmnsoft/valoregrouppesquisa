using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class AuditController(
    IAuditRepository auditRepo,
    ICurrentRequestContext currentRequest,
    ILogger<AuditController> logger) : Controller {

    private Guid? OrganizationId => currentRequest.GetCurrent().EffectiveOrganizationId;

    [HttpGet("/Audit")]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] string? actionFilter,
        [FromQuery] string? entityFilter,
        [FromQuery] string? severityFilter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) {

        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var query = new AuditListQuery(
                Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Action: string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter.Trim(),
                EntityType: string.IsNullOrWhiteSpace(entityFilter) ? null : entityFilter.Trim(),
                Page: Math.Max(1, page),
                PageSize: Math.Clamp(pageSize, 1, 100));

            var (rawItems, total) = await auditRepo.ListAdminAsync(organizationId, query, cancellationToken);

            var list = new List<AuditEventItemViewModel>(rawItems.Count);
            foreach (var item in rawItems) {
                var dict = (IDictionary<string, object?>)item;

                Guid id = dict.TryGetValue("id", out var idVal) && idVal is Guid gId ? gId : Guid.NewGuid();
                string act = dict.TryGetValue("action", out var actVal) ? actVal?.ToString() ?? "unknown" : "unknown";
                string entity = dict.TryGetValue("EntityType", out var entVal) ? entVal?.ToString() ?? "system" : "system";
                string? entityId = dict.TryGetValue("EntityId", out var entIdVal) ? entIdVal?.ToString() : null;
                string message = dict.TryGetValue("message", out var msgVal) ? msgVal?.ToString() ?? "" : "";
                string? metadata = dict.TryGetValue("MetadataJson", out var metaVal) ? metaVal?.ToString() : null;
                string? corr = dict.TryGetValue("CorrelationId", out var corrVal) ? corrVal?.ToString() : null;
                string severity = dict.TryGetValue("severity", out var sevVal) ? sevVal?.ToString() ?? "info" : "info";
                string? module = dict.TryGetValue("module", out var modVal) ? modVal?.ToString() : null;

                DateTimeOffset createdAt = DateTimeOffset.UtcNow;
                if (dict.TryGetValue("CreatedAt", out var dtVal) && dtVal is not null) {
                    if (dtVal is DateTime dt) createdAt = new DateTimeOffset(dt, TimeSpan.Zero);
                    else if (dtVal is DateTimeOffset dto) createdAt = dto;
                }

                string? userDisplay = null;
                if (dict.TryGetValue("UserId", out var uVal) && uVal is not null && Guid.TryParse(uVal.ToString(), out var uId) && uId != Guid.Empty) {
                    userDisplay = $"Usuário #{uId.ToString()[..8]}";
                }
                else {
                    userDisplay = "Sistema / Automático";
                }

                if (!string.IsNullOrWhiteSpace(severityFilter) && !string.Equals(severity, severityFilter, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                list.Add(new AuditEventItemViewModel(
                    id, act, entity, entityId, message, metadata, corr, severity, module, createdAt, userDisplay));
            }

            return View(new AuditIndexViewModel(list, total, page, pageSize, search, actionFilter, entityFilter, severityFilter));
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao carregar trilha de auditoria para a organização {OrganizationId}", organizationId);
            TempData["ErrorMessage"] = "Não foi possível recuperar os eventos de auditoria no momento.";
            return View(new AuditIndexViewModel([], 0, 1, 50));
        }
    }
}
