using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Workspace;

public sealed record WorkspaceItemDto(Guid Id, string ItemType, string Title, string? Summary, string Status,
    string Priority, DateTimeOffset? DueAt, Guid? OwnerUserId, string? SourceType, Guid? SourceId, string? Route,
    DateTimeOffset CreatedAt, bool IsPinned = false);
public sealed record ExecutivePriorityDto(Guid Id, string Title, string? Description, string Status, string Priority,
    Guid? OwnerUserId, DateTimeOffset? DueAt, string? SourceType, Guid? SourceId, int ProgressPercent, DateTimeOffset UpdatedAt);
public sealed record QuickActionDto(string Code, string Label, string Description, string Route, string Icon, int SortOrder);
public sealed record SearchResultDto(Guid Id, string ResultType, string Title, string? Description, string? Route, DateTimeOffset UpdatedAt);
public sealed record ExecutiveWorkspaceDto(IReadOnlyList<WorkspaceItemDto> MyDay, IReadOnlyList<ExecutivePriorityDto> Priorities,
    IReadOnlyList<WorkspaceItemDto> Recent, IReadOnlyList<WorkspaceItemDto> Pinned, IReadOnlyList<QuickActionDto> QuickActions);
public sealed class CreatePriorityRequest
{
    [Required, StringLength(180)] public string Title { get; init; } = "";
    [StringLength(2000)] public string? Description { get; init; }
    [Required, RegularExpression("critical|high|medium|low")] public string Priority { get; init; } = "medium";
    public Guid? OwnerUserId { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    [StringLength(80)] public string? SourceType { get; init; }
    public Guid? SourceId { get; init; }
}
public sealed class PinItemRequest { [Required] public Guid ItemId { get; init; } }

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<WorkspaceItemDto>> MyDayAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct);
    Task<IReadOnlyList<WorkspaceItemDto>> RecentAsync(Guid organizationId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<WorkspaceItemDto>> PinnedAsync(Guid organizationId, Guid userId, CancellationToken ct);
    Task PinAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct);
    Task UnpinAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct);
}
public interface IGlobalSearchRepository { Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid organizationId, Guid userId, string term, bool organizationWide, CancellationToken ct); Task RecordAsync(Guid organizationId, Guid userId, string term, int count, CancellationToken ct); }
public interface IQuickActionRepository { Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid organizationId, CancellationToken ct); Task RecordAsync(Guid organizationId, Guid userId, string code, CancellationToken ct); }
public interface IExecutivePriorityRepository { Task<IReadOnlyList<ExecutivePriorityDto>> ListAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); Task<ExecutivePriorityDto> CreateAsync(Guid organizationId, Guid userId, CreatePriorityRequest request, CancellationToken ct); }

public interface IExecutiveWorkspaceService { Task<ExecutiveWorkspaceDto> GetAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); }
public interface IMyDayService { Task<IReadOnlyList<WorkspaceItemDto>> GetAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); }
public interface IWorkspaceItemService { Task PinAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct); Task UnpinAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct); }
public interface IGlobalSearchService { Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid organizationId, Guid userId, string term, bool organizationWide, CancellationToken ct); }
public interface IQuickActionService { Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid organizationId, CancellationToken ct); Task<QuickActionDto?> ExecuteAsync(Guid organizationId, Guid userId, string code, CancellationToken ct); }
public interface IRecentItemsService { Task<IReadOnlyList<WorkspaceItemDto>> GetAsync(Guid organizationId, Guid userId, CancellationToken ct); }
public interface IPinnedItemsService { Task<IReadOnlyList<WorkspaceItemDto>> GetAsync(Guid organizationId, Guid userId, CancellationToken ct); }
public interface IExecutivePriorityService { Task<IReadOnlyList<ExecutivePriorityDto>> ListAsync(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); Task<ExecutivePriorityDto> CreateAsync(Guid organizationId, Guid userId, CreatePriorityRequest request, CancellationToken ct); }
public interface ICommandPaletteService { Task<IReadOnlyList<QuickActionDto>> ListAsync(Guid organizationId, CancellationToken ct); }
