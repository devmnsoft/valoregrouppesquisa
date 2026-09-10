using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Workspace;

public sealed record WorkspaceItemDto(Guid Id, string ItemType, string Title, string? Summary, string Status,
    string Priority, DateTimeOffset? DueAt, Guid? OwnerUserId, string? SourceType, Guid? SourceId, string? Route,
    DateTimeOffset CreatedAt, bool IsPinned = false);
public sealed record ExecutivePriorityDto(Guid Id, string Title, string? Description, string Status, string Priority,
    Guid? OwnerUserId, string? OwnerName, DateTimeOffset? DueAt, string? SourceType, Guid? SourceId,
    int ProgressPercent, DateTimeOffset UpdatedAt, string ItemType = "priority");
public sealed record PriorityUpdateDto(Guid Id, int ProgressPercent, string? Note, string EventType,
    Guid CreatedBy, string AuthorName, DateTimeOffset CreatedAt);
public sealed record PriorityPermissionsDto(bool CanEdit, bool CanAssign, bool CanUpdateProgress, bool CanComplete,
    bool CanCancel, bool CanReopen);
public sealed record PriorityDetailsDto(ExecutivePriorityDto Priority, IReadOnlyList<PriorityUpdateDto> History,
    PriorityPermissionsDto AllowedActions);
public sealed record PriorityOptionDto(Guid Id, string Label);
public sealed record PrioritySourceOptionDto(Guid Id, string Type, string Label);
public sealed record QuickActionDto(string Code, string Label, string Description, string Route, string Icon, int SortOrder);
public sealed record SearchResultDto(Guid Id, string ResultType, string Title, string? Description, string? Route, DateTimeOffset UpdatedAt);
public sealed record ExecutiveWorkspaceDto(IReadOnlyList<WorkspaceItemDto> MyDay, IReadOnlyList<ExecutivePriorityDto> Priorities,
    IReadOnlyList<WorkspaceItemDto> Recent, IReadOnlyList<WorkspaceItemDto> Pinned, IReadOnlyList<QuickActionDto> QuickActions);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total) {
    public bool HasMore => Page * PageSize < Total;
}
public sealed record PriorityListQuery(int Page = 1, int PageSize = 20, string? Status = null, string? Priority = null,
    Guid? OwnerUserId = null, string? Due = null) {
    public int ValidPage => Math.Max(1, Page);
    public int ValidPageSize => Math.Clamp(PageSize, 1, 100);
}
public sealed record OptionQuery(string? Search = null, int Page = 1, int PageSize = 20, Guid? IncludeId = null) {
    public int ValidPage => Math.Max(1, Page);
    public int ValidPageSize => Math.Clamp(PageSize, 1, 50);
}

public class CreatePriorityRequest {
    [Required, StringLength(180), MinLength(3)] public string Title { get; init; } = "";
    [StringLength(2000)] public string? Description { get; init; }
    [Required, RegularExpression("critical|high|medium|low")] public string Priority { get; init; } = "medium";
    public Guid? OwnerUserId { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    [StringLength(80)] public string? SourceType { get; init; }
    public Guid? SourceId { get; init; }
}
public sealed class UpdatePriorityRequest : CreatePriorityRequest {
    [Required] public DateTimeOffset ExpectedUpdatedAt { get; init; }
}
public sealed class ProgressPriorityRequest {
    [Range(0, 99)] public int ProgressPercent { get; init; }
    [Required, StringLength(1000), MinLength(2)] public string Note { get; init; } = "";
    [Required, StringLength(80)] public string CommandId { get; init; } = "";
    [Required] public DateTimeOffset ExpectedUpdatedAt { get; init; }
}
public sealed class TransitionPriorityRequest {
    [Required, StringLength(1000), MinLength(3)] public string Justification { get; init; } = "";
    [Required, StringLength(80)] public string CommandId { get; init; } = "";
    [Required] public DateTimeOffset ExpectedUpdatedAt { get; init; }
}
public sealed class PinItemRequest { [Required] public Guid ItemId { get; init; } }

public interface IWorkspaceRepository {
    Task<IReadOnlyList<WorkspaceItemDto>> MyDayAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct);
    Task<IReadOnlyList<WorkspaceItemDto>> RecentAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct);
    Task<IReadOnlyList<WorkspaceItemDto>> PinnedAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct);
    Task PinAsync(Guid organizationId, Guid userId, Guid itemId, bool organizationWide, CancellationToken ct);
    Task UnpinAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct);
    Task RecordOpenAsync(Guid organizationId, Guid userId, Guid itemId, bool organizationWide, CancellationToken ct);
}
public interface IGlobalSearchRepository { Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid organizationId, Guid userId, string term, bool organizationWide, CancellationToken ct); Task RecordAsync(Guid organizationId, Guid userId, string term, int count, CancellationToken ct); }
public interface IQuickActionRepository { Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid organizationId, CancellationToken ct); Task RecordAsync(Guid organizationId, Guid userId, string code, CancellationToken ct); }
public interface IExecutivePriorityRepository {
    Task<IReadOnlyList<ExecutivePriorityDto>> ListAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct);
    Task<PageResult<ExecutivePriorityDto>> ListAsync(Guid organizationId, Guid userId, bool organizationWide, PriorityListQuery query, CancellationToken ct);
    Task<PriorityDetailsDto?> GetAsync(Guid organizationId, Guid userId, Guid id, bool organizationWide, bool canManage, CancellationToken ct);
    Task<ExecutivePriorityDto> CreateAsync(Guid organizationId, Guid userId, CreatePriorityRequest request, CancellationToken ct);
    Task<ExecutivePriorityDto> UpdateAsync(Guid organizationId, Guid userId, Guid id, UpdatePriorityRequest request, CancellationToken ct);
    Task<PriorityDetailsDto> ProgressAsync(Guid organizationId, Guid userId, Guid id, ProgressPriorityRequest request, bool organizationWide, bool canManage, CancellationToken ct);
    Task<PriorityDetailsDto> TransitionAsync(Guid organizationId, Guid userId, Guid id, string command, TransitionPriorityRequest request, bool organizationWide, bool canManage, CancellationToken ct);
    Task<PageResult<PriorityOptionDto>> OwnersAsync(Guid organizationId, OptionQuery query, CancellationToken ct);
    Task<PageResult<PrioritySourceOptionDto>> SourcesAsync(Guid organizationId, Guid userId, bool organizationWide, OptionQuery query, CancellationToken ct);
}

public interface IExecutiveWorkspaceService { Task<ExecutiveWorkspaceDto> GetAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); }
public interface IMyDayService { Task<IReadOnlyList<WorkspaceItemDto>> GetAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); }
public interface IWorkspaceItemService { Task PinAsync(Guid organizationId, Guid userId, Guid itemId, bool organizationWide, CancellationToken ct); Task UnpinAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct); Task RecordOpenAsync(Guid organizationId, Guid userId, Guid itemId, bool organizationWide, CancellationToken ct); }
public interface IGlobalSearchService { Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid organizationId, Guid userId, string term, bool organizationWide, CancellationToken ct); }
public interface IQuickActionService { Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid organizationId, CancellationToken ct); Task<QuickActionDto?> ExecuteAsync(Guid organizationId, Guid userId, string code, CancellationToken ct); }
public interface IRecentItemsService { Task<IReadOnlyList<WorkspaceItemDto>> GetAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); }
public interface IPinnedItemsService { Task<IReadOnlyList<WorkspaceItemDto>> GetAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); }
public interface IExecutivePriorityService : IExecutivePriorityRepository { }
public interface ICommandPaletteService { Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid organizationId, CancellationToken ct); }
