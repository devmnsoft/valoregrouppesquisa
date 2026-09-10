using System.ComponentModel.DataAnnotations;

namespace Valora.Application.SuccessCenter;

public sealed record OnboardingStep(Guid Id, string Code, string Title, string Status, Guid? ResponsibleUserId, DateTimeOffset? DueAt, string? Evidence, string? BlockedReason, string Guidance, string ActionUrl);
public sealed record HealthScore(decimal Score, string Level, decimal UsageScore, decimal AdoptionScore, decimal SupportScore, decimal DiagnosticScore, decimal EngagementScore, string RiskLevel, string EvidenceSummary, DateTimeOffset CalculatedAt);
public sealed record SupportTicket(Guid Id, string Subject, string Description, string Category, string Priority, string Status, Guid OpenedByUserId, Guid? AssignedToUserId, DateTimeOffset CreatedAt);
public sealed record TicketMessage(Guid Id, Guid UserId, string Author, string Message, DateTimeOffset CreatedAt);
public sealed record TicketEvent(Guid Id, Guid? UserId, string Author, string EventType, string? FromStatus, string? ToStatus, string? Evidence, DateTimeOffset CreatedAt);
public sealed record SupportTicketDetails(SupportTicket Ticket, IReadOnlyList<TicketMessage> Messages, IReadOnlyList<TicketEvent> Events);
public sealed record SupportTicketQuery(int Page=1,int PageSize=20,string? Status=null,string? Category=null,string? Priority=null,DateOnly? From=null,DateOnly? To=null) { public int ValidPage=>Math.Max(1,Page); public int ValidPageSize=>Math.Clamp(PageSize,1,100); }
public sealed record SupportTicketPage(IReadOnlyList<SupportTicket> Items,int Page,int PageSize,int Total) { public bool HasMore=>Page*PageSize<Total; }
public sealed record KnowledgeArticle(Guid Id, string Title, string Slug, string Summary, string Content, string Visibility, string? MinimumPlan);
public sealed record Playbook(Guid Id, string Name, string Description, string Status);
public sealed record FeatureAdoption(string FeatureCode, long Events, long ActiveUsers, DateTimeOffset? LastUsedAt);
public sealed record UsageEvidence(int LoginDays, int ActiveUsers, int Diagnostics, int Responses, int Reports, int ActionsCreated, int ActionsCompleted, int PendingAlerts, int AiUses, int JourneyUses, int OpenTickets, int OverdueOnboardingSteps);

public sealed class CreateTicketCommand {
    [Required, StringLength(180, MinimumLength = 5)] public string Subject { get; init; } = "";
    [Required, StringLength(4000, MinimumLength = 10)] public string Description { get; init; } = "";
    [Required] public string Category { get; init; } = "";
    [Required] public string Priority { get; init; } = "normal";
}

public interface ISuccessCenterRepository {
    Task EnsureOnboarding(Guid organizationId, CancellationToken ct); Task<IReadOnlyList<OnboardingStep>> GetOnboarding(Guid organizationId, CancellationToken ct);
    Task SetStep(Guid organizationId, Guid userId, Guid stepId, bool complete, string? evidence, CancellationToken ct);
    Task<UsageEvidence> GetUsageEvidence(Guid organizationId, CancellationToken ct); Task SaveHealth(Guid organizationId, HealthScore score, CancellationToken ct);
    Task<IReadOnlyList<SupportTicket>> GetTickets(Guid organizationId, CancellationToken ct); async Task<SupportTicketPage> GetTickets(Guid organizationId, Guid userId, bool organizationWide, SupportTicketQuery query, CancellationToken ct) { var rows=await GetTickets(organizationId,ct); var scoped=organizationWide?rows:rows.Where(x=>x.OpenedByUserId==userId).ToArray(); return new(scoped.Skip((query.ValidPage-1)*query.ValidPageSize).Take(query.ValidPageSize).ToArray(),query.ValidPage,query.ValidPageSize,scoped.Count); } Task<SupportTicketDetails?> GetTicket(Guid organizationId, Guid ticketId, bool includeInternal, CancellationToken ct); Task<SupportTicketDetails?> GetTicket(Guid organizationId, Guid userId, Guid ticketId, bool organizationWide, bool includeInternal, CancellationToken ct)=>GetTicket(organizationId,ticketId,organizationWide&&includeInternal,ct); Task<Guid> CreateTicket(Guid organizationId, Guid userId, CreateTicketCommand command, CancellationToken ct);
    Task AddTicketMessage(Guid organizationId, Guid userId, Guid ticketId, string message, CancellationToken ct); Task SetTicketStatus(Guid organizationId, Guid userId, Guid ticketId, string status, CancellationToken ct);
    Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledge(Guid organizationId, string? query, CancellationToken ct); Task RegisterArticleView(Guid organizationId, Guid userId, Guid articleId, CancellationToken ct);
    Task<IReadOnlyList<Playbook>> GetPlaybooks(Guid organizationId, CancellationToken ct); Task AssignPlaybook(Guid organizationId, Guid userId, Guid playbookId, CancellationToken ct);
    Task RegisterUsage(Guid organizationId, Guid userId, string feature, string? context, CancellationToken ct); Task<IReadOnlyList<FeatureAdoption>> GetAdoption(Guid organizationId, CancellationToken ct);
}

public sealed class OnboardingService(ISuccessCenterRepository repository) { public async Task<IReadOnlyList<OnboardingStep>> Get(Guid organizationId, CancellationToken ct) { await repository.EnsureOnboarding(organizationId, ct); return await repository.GetOnboarding(organizationId, ct); } public Task Complete(Guid o, Guid u, Guid s, string evidence, CancellationToken ct) { if (string.IsNullOrWhiteSpace(evidence)) throw new ValidationException("Informe a evidência da conclusão."); return repository.SetStep(o, u, s, true, evidence.Trim(), ct); } public Task Reopen(Guid o, Guid u, Guid s, CancellationToken ct) => repository.SetStep(o, u, s, false, null, ct); }
public sealed class OrganizationHealthService(ISuccessCenterRepository repository) { public async Task<HealthScore> Calculate(Guid organizationId, CancellationToken ct) { var e = await repository.GetUsageEvidence(organizationId, ct); var usage = Math.Min(100, e.LoginDays * 10 + e.ActiveUsers * 5); var adoption = Math.Min(100, (e.Reports + e.AiUses + e.JourneyUses) * 10); var support = Math.Max(0, 100 - e.OpenTickets * 15); var diagnostic = Math.Min(100, e.Diagnostics * 20 + Math.Min(40, e.Responses)); var engagement = Math.Min(100, e.ActionsCreated * 10 + e.ActionsCompleted * 15); var total = Math.Round((usage + adoption + support + diagnostic + engagement) / 5m, 2); var level = total switch { >= 85 => "excellent", >= 70 => "healthy", >= 50 => "attention", >= 30 => "risk", _ => "critical" }; var risk = e.OverdueOnboardingSteps > 0 ? "onboarding_overdue" : e.OpenTickets > 2 ? "support_demand" : "none"; var evidence = $"logins={e.LoginDays}; active_users={e.ActiveUsers}; diagnostics={e.Diagnostics}; responses={e.Responses}; reports={e.Reports}; actions={e.ActionsCompleted}/{e.ActionsCreated}; alerts={e.PendingAlerts}; ai={e.AiUses}; journey={e.JourneyUses}; open_tickets={e.OpenTickets}; onboarding_overdue={e.OverdueOnboardingSteps}"; var score = new HealthScore(total, level, usage, adoption, support, diagnostic, engagement, risk, evidence, DateTimeOffset.UtcNow); await repository.SaveHealth(organizationId, score, ct); return score; } }
public sealed class SupportTicketService(ISuccessCenterRepository repository) {
    private static readonly IReadOnlySet<string> Categories = new HashSet<string>(["access", "diagnostic", "report", "certificate", "ai", "billing", "integration", "operational_question", "incident"]);
    private static readonly IReadOnlySet<string> Priorities = new HashSet<string>(["low", "normal", "high", "critical"]);
    public Task<IReadOnlyList<SupportTicket>> List(Guid o, CancellationToken ct) => repository.GetTickets(o, ct);
    public Task<SupportTicketPage> List(Guid o,Guid u,bool wide,SupportTicketQuery q,CancellationToken ct) { if(q.Status is not(null or "" or "open" or "in_progress" or "resolved" or "closed" or "cancelled")||q.Priority is not(null or "" or "low" or "normal" or "high" or "critical")||q.From>q.To)throw new ValidationException("Filtros de chamados inválidos."); return repository.GetTickets(o,u,wide,q,ct); }
    public Task<SupportTicketDetails?> Get(Guid o, Guid id, bool includeInternal, CancellationToken ct) => repository.GetTicket(o, id, includeInternal, ct);
    public Task<SupportTicketDetails?> Get(Guid o,Guid u,Guid id,bool wide,bool includeInternal,CancellationToken ct)=>repository.GetTicket(o,u,id,wide,includeInternal,ct);
    public Task<Guid> Create(Guid o, Guid u, CreateTicketCommand c, CancellationToken ct) { if (!Categories.Contains(c.Category)) throw new ValidationException("Categoria inválida."); if (!Priorities.Contains(c.Priority)) throw new ValidationException("Prioridade inválida."); return repository.CreateTicket(o, u, new CreateTicketCommand { Subject = c.Subject.Trim(), Description = c.Description.Trim(), Category = c.Category, Priority = c.Priority }, ct); }
    public Task Reply(Guid o, Guid u, Guid id, string message, CancellationToken ct) => string.IsNullOrWhiteSpace(message) ? throw new ValidationException("Escreva uma resposta.") : repository.AddTicketMessage(o, u, id, message.Trim(), ct);
    public Task Resolve(Guid o, Guid u, Guid id, CancellationToken ct) => repository.SetTicketStatus(o, u, id, "resolved", ct);
    public Task Reopen(Guid o, Guid u, Guid id, CancellationToken ct) => repository.SetTicketStatus(o, u, id, "open", ct);
}
public sealed class KnowledgeBaseService(ISuccessCenterRepository r) { public Task<IReadOnlyList<KnowledgeArticle>> Search(Guid o, string? q, CancellationToken ct) => r.SearchKnowledge(o, q, ct); public Task View(Guid o, Guid u, Guid id, CancellationToken ct) => r.RegisterArticleView(o, u, id, ct); }
public sealed class SuccessPlaybookService(ISuccessCenterRepository r) { public Task<IReadOnlyList<Playbook>> List(Guid o, CancellationToken ct) => r.GetPlaybooks(o, ct); public Task Assign(Guid o, Guid u, Guid id, CancellationToken ct) => r.AssignPlaybook(o, u, id, ct); }
public sealed class ProductUsageService(ISuccessCenterRepository r) { public Task Register(Guid o, Guid u, string feature, string? context, CancellationToken ct) => r.RegisterUsage(o, u, feature, context, ct); }
public sealed class FeatureAdoptionService(ISuccessCenterRepository r) { public Task<IReadOnlyList<FeatureAdoption>> Overview(Guid o, CancellationToken ct) => r.GetAdoption(o, ct); }

public sealed class GenerateOrganizationOnboardingUseCase(OnboardingService s) { public Task<IReadOnlyList<OnboardingStep>> Execute(Guid o, CancellationToken ct) => s.Get(o, ct); }
public sealed class CompleteOnboardingStepUseCase(OnboardingService s) { public Task Execute(Guid o, Guid u, Guid id, string e, CancellationToken ct) => s.Complete(o, u, id, e, ct); }
public sealed class ReopenOnboardingStepUseCase(OnboardingService s) { public Task Execute(Guid o, Guid u, Guid id, CancellationToken ct) => s.Reopen(o, u, id, ct); }
public sealed class CalculateOrganizationHealthScoreUseCase(OrganizationHealthService s) { public Task<HealthScore> Execute(Guid o, CancellationToken ct) => s.Calculate(o, ct); }
public sealed class CreateSupportTicketUseCase(SupportTicketService s) { public Task<Guid> Execute(Guid o, Guid u, CreateTicketCommand c, CancellationToken ct) => s.Create(o, u, c, ct); }
public sealed class ReplySupportTicketUseCase(SupportTicketService s) { public Task Execute(Guid o, Guid u, Guid id, string m, CancellationToken ct) => s.Reply(o, u, id, m, ct); }
public sealed class ResolveSupportTicketUseCase(SupportTicketService s) { public Task Execute(Guid o, Guid u, Guid id, CancellationToken ct) => s.Resolve(o, u, id, ct); }
public sealed class CreateSuccessPlaybookUseCase(SuccessPlaybookService s) { public Task<IReadOnlyList<Playbook>> Execute(Guid o, CancellationToken ct) => s.List(o, ct); }
public sealed class AssignPlaybookToOrganizationUseCase(SuccessPlaybookService s) { public Task Execute(Guid o, Guid u, Guid id, CancellationToken ct) => s.Assign(o, u, id, ct); }
public sealed class RegisterProductUsageEventUseCase(ProductUsageService s) { public Task Execute(Guid o, Guid u, string f, string? c, CancellationToken ct) => s.Register(o, u, f, c, ct); }
public sealed class GenerateFeatureAdoptionOverviewUseCase(FeatureAdoptionService s) { public Task<IReadOnlyList<FeatureAdoption>> Execute(Guid o, CancellationToken ct) => s.Overview(o, ct); }
