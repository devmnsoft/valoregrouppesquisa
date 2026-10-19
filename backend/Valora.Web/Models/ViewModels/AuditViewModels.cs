namespace Valora.Web.Models.ViewModels;

public sealed record AuditEventItemViewModel(
    Guid Id,
    string Action,
    string EntityType,
    string? EntityId,
    string Message,
    string? MetadataJson,
    string? CorrelationId,
    string Severity,
    string? Module,
    DateTimeOffset CreatedAt,
    string? UserDisplay) {

    public string SeverityBadgeClass => (Severity ?? "").ToLowerInvariant() switch {
        "critical" => "text-bg-danger",
        "error" => "text-bg-danger",
        "warning" => "text-bg-warning text-dark",
        "high" => "text-bg-warning text-dark",
        "info" => "text-bg-info text-white",
        _ => "text-bg-secondary"
    };

    public string ActionIcon {
        get {
            var a = (Action ?? "").ToLowerInvariant();
            if (a.Contains("delete") || a.Contains("remove")) return "bi-trash text-danger";
            if (a.Contains("update") || a.Contains("modify") || a.Contains("edit")) return "bi-pencil-square text-warning";
            if (a.Contains("create") || a.Contains("add") || a.Contains("insert")) return "bi-plus-circle text-success";
            if (a.Contains("export")) return "bi-download text-primary";
            if (a.Contains("login") || a.Contains("auth")) return "bi-shield-lock text-info";
            return "bi-activity text-secondary";
        }
    }
}

public sealed class AuditIndexViewModel {
    public AuditIndexViewModel(
        IReadOnlyList<AuditEventItemViewModel> items,
        long totalCount,
        int page,
        int pageSize,
        string? search = null,
        string? actionFilter = null,
        string? entityFilter = null,
        string? severityFilter = null) {
        Items = items;
        TotalCount = totalCount;
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
        Search = search;
        ActionFilter = actionFilter;
        EntityFilter = entityFilter;
        SeverityFilter = severityFilter;
        TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        Last24HoursCount = items.Count(i => i.CreatedAt >= DateTimeOffset.UtcNow.AddHours(-24));
        CriticalCount = items.Count(i => string.Equals(i.Severity, "critical", StringComparison.OrdinalIgnoreCase) || string.Equals(i.Severity, "error", StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<AuditEventItemViewModel> Items { get; }
    public long TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public string? Search { get; }
    public string? ActionFilter { get; }
    public string? EntityFilter { get; }
    public string? SeverityFilter { get; }

    public int Last24HoursCount { get; }
    public int CriticalCount { get; }

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
