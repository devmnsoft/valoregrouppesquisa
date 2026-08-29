using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Knowledge;

public static class KnowledgeStatuses { public const string Draft="draft", Review="review", Published="published", Archived="archived"; public static readonly string[] All=[Draft,Review,Published,Archived]; }
public sealed record KnowledgeSummary(int PublishedArticles,int ArticlesInReview,int ActivePlaybooks,int RecentLessons,int LearningPaths,int DictionaryTerms);
public sealed record KnowledgeCategory(Guid Id,string Name,string Code);
public sealed record KnowledgeArticle(Guid Id,Guid CategoryId,string Category,string Title,string Status,string Content,Guid ResponsibleUserId,bool AiAssisted,int Version,DateTimeOffset UpdatedAt);
public sealed record Playbook(Guid Id,string Title,string Objective,string Area,string Status,int StepCount,DateTimeOffset UpdatedAt);
public sealed record OrganizationalLesson(Guid Id,string Title,string Learning,string OriginType,string OriginReference,string Impact,string? Evidence,string Status,bool Critical,DateTimeOffset CreatedAt);
public sealed record LearningPath(Guid Id,string Title,string Objective,string Status,int ItemCount,int AssignmentCount,int CompletionPercent);
public sealed record CognitiveTerm(Guid Id,string Code,string Name,string Definition,string Category,string MethodologicalUse,string Status,DateTimeOffset UpdatedAt);
public sealed record KnowledgeArticleVersion(Guid Id,Guid ArticleId,int Version,string Title,string Content,Guid CreatedByUserId,DateTimeOffset CreatedAt);

public sealed class CreateArticleRequest {
 [Required,StringLength(180,MinimumLength=3)] public string Title {get;set;}="";
 [Required] public Guid CategoryId {get;set;}
 [Required,StringLength(20000,MinimumLength=20)] public string Content {get;set;}="";
 [Required] public Guid ResponsibleUserId {get;set;}
 public bool AiAssisted {get;set;}
}
public sealed class CreatePlaybookRequest {
 [Required,StringLength(180)] public string Title {get;set;}="";
 [Required,StringLength(1000,MinimumLength=10)] public string Objective {get;set;}="";
 [Required,StringLength(100)] public string Area {get;set;}="";
 [Required,MinLength(1)] public List<CreatePlaybookStepRequest> Steps {get;set;}=[];
}
public sealed class CreatePlaybookStepRequest { [Range(1,999)] public int Order {get;set;} [Required,StringLength(1000)] public string Description {get;set;}=""; [Required,StringLength(180)] public string SuggestedOwner {get;set;}=""; }
public sealed class CreateLessonRequest {
 [Required,StringLength(180)] public string Title {get;set;}=""; [Required,StringLength(4000,MinimumLength=10)] public string Learning {get;set;}="";
 [Required,RegularExpression("action|decision|diagnostic|meeting|report|incident")] public string OriginType {get;set;}="";
 [Required,StringLength(300)] public string OriginReference {get;set;}=""; [Required,StringLength(1000)] public string Impact {get;set;}="";
 public string? Evidence {get;set;} public bool Critical {get;set;}
}
public sealed class CreateLearningPathRequest { [Required,StringLength(180)] public string Title {get;set;}=""; [Required,StringLength(1000)] public string Objective {get;set;}=""; [Required,MinLength(1)] public List<Guid> ArticleIds {get;set;}=[]; }
public sealed class CreateCognitiveTermRequest { [Required,RegularExpression("[a-z0-9._-]+"),StringLength(80)] public string Code {get;set;}=""; [Required,StringLength(160)] public string Name {get;set;}=""; [Required,StringLength(4000,MinimumLength=10)] public string Definition {get;set;}=""; [Required,StringLength(100)] public string Category {get;set;}=""; [Required,StringLength(2000,MinimumLength=10)] public string MethodologicalUse {get;set;}=""; }

public interface IKnowledgeRepository {
 Task<KnowledgeSummary> SummaryAsync(Guid organizationId,CancellationToken ct); Task<IReadOnlyList<KnowledgeCategory>> CategoriesAsync(Guid organizationId,CancellationToken ct);
 Task<IReadOnlyList<KnowledgeArticle>> ArticlesAsync(Guid organizationId,string? query,string? status,Guid? categoryId,CancellationToken ct); Task<KnowledgeArticle?> ArticleAsync(Guid organizationId,Guid id,CancellationToken ct); Task<KnowledgeArticle> CreateArticleAsync(Guid organizationId,CreateArticleRequest request,Guid actor,string correlationId,CancellationToken ct); Task<KnowledgeArticle> TransitionArticleAsync(Guid organizationId,Guid id,string status,Guid actor,string correlationId,CancellationToken ct); Task<IReadOnlyList<KnowledgeArticleVersion>> VersionsAsync(Guid organizationId,Guid articleId,CancellationToken ct);
 Task<IReadOnlyList<Playbook>> PlaybooksAsync(Guid organizationId,CancellationToken ct); Task<Playbook> CreatePlaybookAsync(Guid organizationId,CreatePlaybookRequest request,Guid actor,string correlationId,CancellationToken ct);
 Task<IReadOnlyList<OrganizationalLesson>> LessonsAsync(Guid organizationId,CancellationToken ct); Task<OrganizationalLesson> CreateLessonAsync(Guid organizationId,CreateLessonRequest request,Guid actor,string correlationId,CancellationToken ct);
 Task<IReadOnlyList<LearningPath>> LearningPathsAsync(Guid organizationId,CancellationToken ct); Task<LearningPath> CreateLearningPathAsync(Guid organizationId,CreateLearningPathRequest request,Guid actor,string correlationId,CancellationToken ct);
 Task<IReadOnlyList<CognitiveTerm>> TermsAsync(Guid organizationId,CancellationToken ct); Task<CognitiveTerm> CreateTermAsync(Guid organizationId,CreateCognitiveTermRequest request,Guid actor,string correlationId,CancellationToken ct);
}
