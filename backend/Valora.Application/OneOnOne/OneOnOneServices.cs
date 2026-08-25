namespace Valora.Application.OneOnOne;

public sealed record OneOnOneSessionDto(Guid Id, Guid OrganizationId, Guid LeaderUserId, Guid ParticipantUserId, string LeaderName, string ParticipantName, Guid? DiagnosticId, Guid? ResultId, string Title, string Status, DateTime? ScheduledAt, DateTime? StartedAt, DateTime? CompletedAt, string? Summary, string? EvidenceSummary, string? AiSummary, Guid CreatedByUserId, DateTime CreatedAt, int PendingCommitments = 0);
public sealed record CreateOneOnOneSessionRequest(Guid LeaderUserId, Guid ParticipantUserId, DateTime? ScheduledAt, string Title, Guid? DiagnosticId, Guid? ResultId, string? EvidenceSummary);
public sealed record CompleteOneOnOneSessionRequest(string Summary, string EvidenceSummary);
public sealed record OneOnOneAgendaItemDto(Guid Id, Guid SessionId, string Title, string? Description, string SourceType, string? Evidence, int SortOrder, bool Discussed);
public sealed record OneOnOneCommitmentDto(Guid Id, Guid SessionId, Guid ResponsibleUserId, string Title, string? Description, DateTime DueAt, string Status, Guid? ActionItemId, DateTime CreatedAt);
public sealed record OneOnOneFeedbackDto(Guid Id, Guid SessionId, Guid FromUserId, Guid ToUserId, string Feedback, string Evidence, DateTime CreatedAt);
public sealed record PrivateNoteDto(Guid Id, Guid SessionId, Guid CreatedByUserId, string Note, DateTime CreatedAt);
public sealed record AiSuggestionDto(Guid Id, Guid? SessionId, Guid? LeaderUserId, string SuggestionType, string Observation, string Evidence, string Impact, string Recommendation, string Limitation, string Status, DateTime CreatedAt);
public sealed record LeadershipProfileDto(Guid Id, Guid UserId, string DisplayName, string? RoleTitle, string? DevelopmentSummary, DateTime? LastSessionAt, int PendingCommitments, IReadOnlyList<AiSuggestionDto>? Suggestions = null);
public sealed record OneOnOneDashboardDto(IReadOnlyList<OneOnOneSessionDto> Upcoming, IReadOnlyList<OneOnOneSessionDto> Overdue, IReadOnlyList<OneOnOneCommitmentDto> Commitments, IReadOnlyList<LeadershipProfileDto> Leaders, IReadOnlyList<AiSuggestionDto> Suggestions);
public sealed record OneOnOneDetailsDto(OneOnOneSessionDto Session, IReadOnlyList<OneOnOneAgendaItemDto> Agenda, IReadOnlyList<OneOnOneCommitmentDto> Commitments, IReadOnlyList<OneOnOneFeedbackDto> Feedbacks, IReadOnlyList<PrivateNoteDto> PrivateNotes, IReadOnlyList<AiSuggestionDto> Suggestions);

public interface IOneOnOneRepository
{
    Task<OneOnOneDashboardDto> Dashboard(Guid organizationId, Guid viewerId, bool canReadAllPrivateNotes, CancellationToken ct);
    Task<IReadOnlyList<OneOnOneSessionDto>> List(Guid organizationId, CancellationToken ct);
    Task<OneOnOneDetailsDto?> Get(Guid organizationId, Guid id, Guid viewerId, bool canReadAllPrivateNotes, CancellationToken ct);
    Task<Guid> Create(Guid organizationId, Guid userId, CreateOneOnOneSessionRequest request, CancellationToken ct);
    Task Schedule(Guid organizationId, Guid id, DateTime scheduledAt, CancellationToken ct);
    Task Complete(Guid organizationId, Guid id, Guid userId, CompleteOneOnOneSessionRequest request, CancellationToken ct);
    Task AddAgenda(Guid organizationId, Guid sessionId, IReadOnlyList<OneOnOneAgendaItemDto> items, CancellationToken ct);
    Task<Guid> AddCommitment(Guid organizationId, Guid userId, Guid sessionId, Guid responsibleId, string title, string? description, DateTime dueAt, bool createAction, CancellationToken ct);
    Task AddFeedback(Guid organizationId, Guid userId, Guid sessionId, Guid toUserId, string feedback, string evidence, CancellationToken ct);
    Task AddPrivateNote(Guid organizationId, Guid userId, Guid sessionId, string note, CancellationToken ct);
}
public interface ILeadershipProfileRepository { Task<IReadOnlyList<LeadershipProfileDto>> List(Guid organizationId,CancellationToken ct); Task<LeadershipProfileDto?> Get(Guid organizationId,Guid userId,CancellationToken ct); }
public interface ILeadershipDevelopmentRepository { Task<Guid> Snapshot(Guid organizationId,Guid leaderUserId,Guid? sessionId,Guid userId,string evidence,CancellationToken ct); }

public sealed class OneOnOneSessionService(IOneOnOneRepository repository)
{
    public Task<OneOnOneDashboardDto> Dashboard(Guid o,Guid u,bool privateAccess,CancellationToken c)=>repository.Dashboard(o,u,privateAccess,c);
    public Task<IReadOnlyList<OneOnOneSessionDto>> List(Guid o,CancellationToken c)=>repository.List(o,c);
    public Task<OneOnOneDetailsDto?> Get(Guid o,Guid id,Guid u,bool privateAccess,CancellationToken c)=>repository.Get(o,id,u,privateAccess,c);
    public Task<Guid> Create(Guid o,Guid u,CreateOneOnOneSessionRequest r,CancellationToken c){if(r.LeaderUserId==Guid.Empty||r.ParticipantUserId==Guid.Empty)throw new ArgumentException("Líder e participante são obrigatórios.");if(string.IsNullOrWhiteSpace(r.Title))throw new ArgumentException("O objetivo da sessão é obrigatório.");return repository.Create(o,u,r with{Title=r.Title.Trim()},c);}
    public Task Schedule(Guid o,Guid id,DateTime at,CancellationToken c){if(at==default)throw new ArgumentException("Informe a data da reunião.");return repository.Schedule(o,id,at,c);}
    public Task Complete(Guid o,Guid id,Guid u,CompleteOneOnOneSessionRequest r,CancellationToken c){if(string.IsNullOrWhiteSpace(r.Summary)||r.Summary.Trim().Length<20)throw new ArgumentException("A sessão concluída exige um resumo com pelo menos 20 caracteres.");if(string.IsNullOrWhiteSpace(r.EvidenceSummary))throw new ArgumentException("Registre a evidência que sustenta o resumo.");return repository.Complete(o,id,u,r,c);}
}
public sealed class OneOnOneAgendaService(IOneOnOneRepository repository){public Task Generate(Guid o,Guid id,string evidence,CancellationToken c){var safe=string.IsNullOrWhiteSpace(evidence)?"Sem evidência adicional disponível; validar durante a conversa.":evidence.Trim();IReadOnlyList<OneOnOneAgendaItemDto> items=[new(Guid.NewGuid(),id,"Abertura e contexto","Como você está e o que precisa desta conversa?","facilitated",safe,1,false),new(Guid.NewGuid(),id,"Evolução e evidências","O que mudou desde o último acompanhamento?","evidence",safe,2,false),new(Guid.NewGuid(),id,"Compromissos e apoio","Quais próximos passos têm responsável e prazo?","commitments",safe,3,false)];return repository.AddAgenda(o,id,items,c);}}
public sealed class OneOnOneCommitmentService(IOneOnOneRepository r){public Task<Guid> Register(Guid o,Guid u,Guid s,Guid responsible,string title,string? description,DateTime due,bool action,CancellationToken c){if(responsible==Guid.Empty||due==default||string.IsNullOrWhiteSpace(title))throw new ArgumentException("Compromisso exige responsável, descrição e prazo.");return r.AddCommitment(o,u,s,responsible,title.Trim(),description,due,action,c);}}
public sealed class OneOnOneAiSuggestionService{public AiSuggestionDto Suggest(Guid? session,Guid? leader,string type,string observation,string evidence,string impact,string recommendation,string? limitation=null){if(string.IsNullOrWhiteSpace(evidence))throw new ArgumentException("Sugestões de IA exigem evidência verificável.");return new(Guid.NewGuid(),session,leader,type,observation.Trim(),evidence.Trim(),impact.Trim(),recommendation.Trim(),limitation??"Sugestão de apoio; requer validação humana e não constitui diagnóstico individual.","proposed",DateTime.UtcNow);}}
public sealed class LeadershipProfileService(ILeadershipProfileRepository r){public Task<IReadOnlyList<LeadershipProfileDto>> List(Guid o,CancellationToken c)=>r.List(o,c);public Task<LeadershipProfileDto?> Get(Guid o,Guid id,CancellationToken c)=>r.Get(o,id,c);}
public sealed class LeadershipDevelopmentService(ILeadershipDevelopmentRepository r){public Task<Guid> Snapshot(Guid o,Guid leader,Guid? session,Guid u,string evidence,CancellationToken c){if(string.IsNullOrWhiteSpace(evidence))throw new ArgumentException("Snapshot exige evidência.");return r.Snapshot(o,leader,session,u,evidence,c);}}
public sealed class CreateOneOnOneSessionUseCase(OneOnOneSessionService s){public Task<Guid> Execute(Guid o,Guid u,CreateOneOnOneSessionRequest r,CancellationToken c)=>s.Create(o,u,r,c);}
public sealed class ScheduleOneOnOneSessionUseCase(OneOnOneSessionService s){public Task Execute(Guid o,Guid id,DateTime at,CancellationToken c)=>s.Schedule(o,id,at,c);}
public sealed class CompleteOneOnOneSessionUseCase(OneOnOneSessionService s){public Task Execute(Guid o,Guid id,Guid u,CompleteOneOnOneSessionRequest r,CancellationToken c)=>s.Complete(o,id,u,r,c);}
public sealed class GenerateOneOnOneAgendaUseCase(OneOnOneAgendaService s){public Task Execute(Guid o,Guid id,string evidence,CancellationToken c)=>s.Generate(o,id,evidence,c);}
public sealed class RegisterOneOnOneCommitmentUseCase(OneOnOneCommitmentService s){public Task<Guid> Execute(Guid o,Guid u,Guid id,Guid responsible,string title,string? description,DateTime due,bool action,CancellationToken c)=>s.Register(o,u,id,responsible,title,description,due,action,c);}
public sealed class RegisterLeadershipFeedbackUseCase(IOneOnOneRepository r){public Task Execute(Guid o,Guid u,Guid id,Guid to,string feedback,string evidence,CancellationToken c){if(string.IsNullOrWhiteSpace(feedback)||string.IsNullOrWhiteSpace(evidence))throw new ArgumentException("Feedback construtivo exige conteúdo e evidência.");return r.AddFeedback(o,u,id,to,feedback.Trim(),evidence.Trim(),c);}}
public sealed class GenerateLeadershipDevelopmentSnapshotUseCase(LeadershipDevelopmentService s){public Task<Guid> Execute(Guid o,Guid leader,Guid? session,Guid u,string evidence,CancellationToken c)=>s.Snapshot(o,leader,session,u,evidence,c);}
