using Valora.Application.Workspace;

namespace Valora.Application.ActionCenter;

public static class ActionStatuses {
    public static readonly IReadOnlySet<string> Plan = new HashSet<string>(["draft", "proposed", "approved", "in_execution", "completed", "canceled"]);
    public static readonly IReadOnlySet<string> Item = new HashSet<string>(["pending", "in_progress", "blocked", "completed", "canceled", "overdue"]);
    public static readonly IReadOnlySet<string> Priorities = new HashSet<string>(["critical", "high", "medium", "low"]);
    public static string Label(string? value) => value switch {
        "draft" => "Rascunho", "proposed" => "Proposto", "approved" => "Aprovado",
        "in_execution" or "in_progress" => "Em execução", "pending" => "Pendente",
        "blocked" => "Bloqueada", "overdue" => "Atrasada", "completed" => "Concluída",
        "canceled" or "cancelled" => "Cancelada", "critical" => "Crítica", "high" => "Alta",
        "medium" => "Média", "low" => "Baixa", null or "" => "Não informada", _ => value
    };
}
public static class ActionPlanClosurePolicy {
    public static bool CanComplete(string? status, int pendingConditionCount) =>
        status == "in_execution" && pendingConditionCount == 0;
}
public enum ActionAssignmentFilter { Assigned, Unassigned }
public enum ActionPlanScopeFilter { Active }
public enum ActionItemScopeFilter { Open }
public sealed record ActionPlanDto(Guid Id, string Title, string Summary, string OriginType, string Status, string Priority, Guid? OwnerUserId, DateTime? DueAt, string EvidenceSummary, string ExpectedOutcome, int ProgressPercent, DateTime CreatedAt, string? OwnerName = null, int ActiveItemCount = 0, long Version = 1, DateTime? ApprovedAt = null, long? ApprovedVersion = null, DateTime? ActualStartedAt = null, DateTime? CompletedAt = null, string? CompletionResult = null, string? CompletionEvidence = null, string? OrganizationName = null, string? OrganizationTimeZone = null, string? ApprovedByName = null, string? StartedByName = null, string? CompletedByName = null, string? CancellationReason = null);
public sealed record ActionPlanReadinessDestination(string Code,string Label,string UrlFragment);
public sealed record ActionPlanClosureCheck(
    string Stage,
    IReadOnlyList<string> AllowedActions,
    IReadOnlyList<string> Pending,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ActionPlanReadinessDestination> Destinations,
    int TotalActivities,
    int OpenActivities,
    int BlockedActivities,
    int CompletedActivities,
    int CanceledActivities,
    string ExpectedOutcome,
    string? CompletionResult,
    string? CompletionEvidence,
    bool CanComplete);
public sealed record ActionItemDto(Guid Id, Guid ActionPlanId, string Title, string Description, string OriginType, Guid? OriginId, string? RelatedDimension, string? RelatedIndexCode, string Priority, string Status, Guid? ResponsibleUserId, DateTime? DueAt, DateTime? CompletedAt, int ProgressPercent, string EvidenceSummary, string ExpectedOutcome, string? CompletionEvidence, string? AiRecommendationSummary, DateTime CreatedAt, long Version = 1, string? ResponsibleName = null, string? PlanTitle = null, string? CompletionResult = null);
public sealed record ActionHistoryDto(Guid Id, string? FromStatus, string ToStatus, int? ProgressPercent, string? Reason, string? AuthorName, DateTime ChangedAt, string? Operation = null, string? FromValue = null, string? ToValue = null, long? IntentVersion = null);
public sealed record ActionPlanHistoryDto(Guid Id, string Operation, string? FromValue, string? ToValue, string? Reason, string? AuthorName, DateTime ChangedAt, long? IntentVersion);
public sealed record ActionItemDetailsDto(ActionItemDto Item, IReadOnlyList<ActionHistoryDto> History, int HistoryPage, int HistoryPages, bool CanEdit, bool CanAssign, bool CanReschedule, bool CanProgress, bool CanBlock, bool CanResume, bool CanComplete);
public sealed record ActionDashboardDto(int Critical, int Overdue, int Unassigned, int ActivePlans, int Blocked, IReadOnlyList<ActionItemDto> Upcoming, IReadOnlyList<ActionPlanDto> Plans);
public sealed record ActionPlanListQuery(int Page=1,int PageSize=20,string? Search=null,string? Status=null,Guid? OwnerUserId=null,string? Priority=null,string? Due=null,string? Sort=null,ActionPlanScopeFilter? Scope=null) {
    public int ValidPage=>Math.Max(1,Page); public int ValidPageSize=>Math.Clamp(PageSize,1,50);
}
public sealed record ActionItemListQuery(int Page=1,int PageSize=20,string? Search=null,string? Status=null,Guid? ResponsibleUserId=null,string? Priority=null,string? Due=null,ActionAssignmentFilter? Assignment=null,ActionItemScopeFilter? Scope=null) {
    public int ValidPage=>Math.Max(1,Page); public int ValidPageSize=>Math.Clamp(PageSize,1,50);
}
public sealed class ProgressActionRequest { [System.ComponentModel.DataAnnotations.Range(0,99)] public int ProgressPercent { get; init; } [System.ComponentModel.DataAnnotations.StringLength(1000)] public string? Note { get; init; } [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class TransitionActionRequest { [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(1000,MinimumLength=3)] public string Reason { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class CompleteActionRequest { [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=10)] public string Result { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] public string Evidence { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public class PlanTransitionRequest { [System.ComponentModel.DataAnnotations.StringLength(1000)] public string? Note { get; init; } [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class CompleteActionPlanRequest : PlanTransitionRequest { [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=10)] public string Result { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] public string Evidence { get; init; } = ""; [System.ComponentModel.DataAnnotations.Range(typeof(bool),"true","true",ErrorMessage="Confirme a revisão do resultado e das evidências.")] public bool Confirmed { get; init; } }
public sealed class EditActionItemRequest { [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(180,MinimumLength=3)] public string Title { get; init; } = ""; [System.ComponentModel.DataAnnotations.StringLength(2000)] public string Description { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.RegularExpression("critical|high|medium|low")] public string Priority { get; init; } = "medium"; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] public string ExpectedOutcome { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class AssignResponsibleRequest { public Guid? ResponsibleUserId { get; init; } [System.ComponentModel.DataAnnotations.StringLength(1000)] public string? Reason { get; init; } [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class RescheduleActionRequest { [System.ComponentModel.DataAnnotations.Required] public DateTime? DueAt { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(1000,MinimumLength=3)] public string Reason { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class EditActionPlanRequest { [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(180,MinimumLength=3)] public string Title { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] public string Summary { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.RegularExpression("critical|high|medium|low")] public string Priority { get; init; } = "medium"; [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] public string ExpectedOutcome { get; init; } = ""; [System.ComponentModel.DataAnnotations.Required] public long Version { get; init; } [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed record CreateActionPlanRequest(
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(180,MinimumLength=3)] string Title,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] string Summary,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.RegularExpression("manual|diagnostic|result|ai_insight|alert|decision")] string OriginType,
    Guid? OriginId, Guid? DiagnosticId, Guid? ResultId, Guid? GovernanceCycleId,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.RegularExpression("critical|high|medium|low")] string Priority,
    Guid? OwnerUserId, DateTime? StartsAt, DateTime? DueAt,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] string EvidenceSummary,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] string ExpectedOutcome,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] string CommandId = "",
    long? ExpectedOriginVersion = null,
    [property:System.ComponentModel.DataAnnotations.StringLength(1000,MinimumLength=10)] string? AdditionalInitiativeReason = null);
public sealed record CreateActionItemRequest(
    [property:System.ComponentModel.DataAnnotations.Required] Guid ActionPlanId,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(180,MinimumLength=3)] string Title,
    [property:System.ComponentModel.DataAnnotations.StringLength(2000)] string Description,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.RegularExpression("manual|diagnostic|result|ai_insight|alert|decision")] string OriginType,
    Guid? OriginId, Guid? DiagnosticId, Guid? ResultId,
    [property:System.ComponentModel.DataAnnotations.StringLength(160)] string? RelatedDimension,
    [property:System.ComponentModel.DataAnnotations.StringLength(80)] string? RelatedIndexCode,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.RegularExpression("critical|high|medium|low")] string Priority,
    Guid? ResponsibleUserId, DateTime? DueAt,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] string EvidenceSummary,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(2000,MinimumLength=3)] string ExpectedOutcome,
    [property:System.ComponentModel.DataAnnotations.StringLength(2000)] string? AiRecommendationSummary,
    [property:System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(80)] string CommandId = "");
public sealed record PriorityActionDto(Guid ActionId, Guid PlanId, string PlanTitle, string Title, string Status, string Priority, Guid? ResponsibleUserId, DateTime? DueAt, int ProgressPercent, string EvidenceSummary, string ExpectedOutcome, string? CompletionEvidence, DateTimeOffset LinkedAt);
public sealed record ActionOptionDto(Guid Id, string Title, string Status, string Priority, string? Context, Guid? ResponsibleUserId, string? ResponsibleName);
public sealed class LinkPriorityActionRequest { [System.ComponentModel.DataAnnotations.Required] public Guid ActionId { get; init; } [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = ""; }
public sealed class CreatePriorityActionRequest {
    public Guid? PlanId { get; init; } public bool CreatePlan { get; init; }
    [System.ComponentModel.DataAnnotations.StringLength(180)] public string? PlanTitle { get; init; }
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(180, MinimumLength=3)] public string Title { get; init; } = "";
    [System.ComponentModel.DataAnnotations.StringLength(2000)] public string Description { get; init; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(2000, MinimumLength=3)] public string ExpectedOutcome { get; init; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(2000, MinimumLength=3)] public string EvidenceSummary { get; init; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression("critical|high|medium|low")] public string Priority { get; init; } = "medium";
    public Guid? ResponsibleUserId { get; init; } public DateTime? DueAt { get; init; }
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(80)] public string CommandId { get; init; } = "";
}
public interface IPriorityActionRepository { Task<IReadOnlyList<PriorityActionDto>> List(Guid organizationId, Guid priorityId, Guid userId, bool wide, CancellationToken ct); Task<Guid> Link(Guid organizationId, Guid userId, Guid priorityId, LinkPriorityActionRequest request, bool wide, CancellationToken ct); Task<Guid> Create(Guid organizationId, Guid userId, Guid priorityId, CreatePriorityActionRequest request, bool wide, CancellationToken ct); }
public interface IActionPlanRepository { Task<ActionDashboardDto> Dashboard(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); Task<IReadOnlyList<ActionPlanDto>> List(Guid organizationId, Guid userId, bool organizationWide, CancellationToken ct); Task<PageResult<ActionPlanDto>> List(Guid organizationId,Guid userId,bool organizationWide,ActionPlanListQuery query,CancellationToken ct); Task<PageResult<ActionOptionDto>> Options(Guid organizationId, Guid userId, bool organizationWide, OptionQuery query, CancellationToken ct); Task<ActionPlanDto?> Get(Guid organizationId, Guid userId, Guid id, bool organizationWide, CancellationToken ct); Task<ActionPlanClosureCheck?> ClosureCheck(Guid organizationId,Guid userId,Guid id,bool organizationWide,CancellationToken ct); Task<PageResult<ActionPlanHistoryDto>> History(Guid organizationId,Guid userId,Guid id,bool organizationWide,int page,string? operation,CancellationToken ct); Task<Guid> Create(Guid organizationId, Guid userId, bool globalAdministrator, CreateActionPlanRequest request, CancellationToken ct); Task Edit(Guid organizationId,Guid userId,Guid id,bool organizationWide,EditActionPlanRequest request,CancellationToken ct); Task Assign(Guid organizationId,Guid userId,Guid id,bool organizationWide,AssignResponsibleRequest request,CancellationToken ct); Task Reschedule(Guid organizationId,Guid userId,Guid id,bool organizationWide,RescheduleActionRequest request,CancellationToken ct); Task Transition(Guid organizationId,Guid userId,Guid id,bool organizationWide,string operation,PlanTransitionRequest request,CancellationToken ct); Task Complete(Guid organizationId,Guid userId,Guid id,bool organizationWide,CompleteActionPlanRequest request,CancellationToken ct); }
public interface IActionItemRepository { Task<PageResult<ActionItemDto>> List(Guid organizationId, Guid userId, Guid? planId, bool organizationWide, ActionItemListQuery query, CancellationToken ct); Task<PageResult<ActionOptionDto>> Options(Guid organizationId, Guid userId, bool organizationWide, OptionQuery query, CancellationToken ct); Task<PageResult<ActionOptionDto>> VisibleResponsibleOptions(Guid organizationId, Guid userId, bool organizationWide, OptionQuery query, CancellationToken ct); Task<PageResult<ActionOptionDto>> ResponsibleOptions(Guid organizationId, OptionQuery query, CancellationToken ct); Task<ActionItemDto?> Get(Guid organizationId, Guid userId, Guid id, bool organizationWide, CancellationToken ct); Task<ActionItemDetailsDto?> Details(Guid organizationId, Guid userId, Guid id, bool organizationWide, bool canManage, bool canComplete, int historyPage, CancellationToken ct); Task<Guid> Create(Guid organizationId, Guid userId, CreateActionItemRequest request, CancellationToken ct); Task Edit(Guid organizationId,Guid userId,Guid id,bool organizationWide,EditActionItemRequest request,CancellationToken ct); Task Assign(Guid organizationId,Guid userId,Guid id,bool organizationWide,AssignResponsibleRequest request,CancellationToken ct); Task Reschedule(Guid organizationId,Guid userId,Guid id,bool organizationWide,RescheduleActionRequest request,CancellationToken ct); Task UpdateProgress(Guid organizationId, Guid userId, Guid id, bool organizationWide, ProgressActionRequest request, CancellationToken ct); Task Block(Guid organizationId, Guid userId, Guid id, bool organizationWide, TransitionActionRequest request, CancellationToken ct); Task Resume(Guid organizationId, Guid userId, Guid id, bool organizationWide, TransitionActionRequest request,CancellationToken ct); Task Cancel(Guid organizationId,Guid userId,Guid id,bool organizationWide,TransitionActionRequest request,CancellationToken ct); Task Complete(Guid organizationId, Guid userId, Guid id, bool organizationWide, CompleteActionRequest request, CancellationToken ct); }
public interface IRecommendationRepository { Task<IReadOnlyList<ActionRecommendationDto>> List(Guid organizationId, CancellationToken ct); Task<Guid> Enqueue(Guid organizationId, Guid userId, ActionRecommendationDto recommendation, CancellationToken ct); }
public sealed record ActionRecommendationDto(Guid Id, string SourceType, Guid? SourceId, string Observation, string Evidence, string Correlation, string Impact, string Priority, string Recommendation, string Limitation, string Status);
