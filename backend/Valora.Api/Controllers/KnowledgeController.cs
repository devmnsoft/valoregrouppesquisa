using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Knowledge;
namespace Valora.Api.Controllers;

[Authorize,ApiController,Route("api/v1/knowledge")]
public sealed class KnowledgeController(KnowledgeOverviewService overview,KnowledgeArticleService articles,PlaybookService playbooks,OrganizationalLessonService lessons,LearningPathService paths,CognitiveDictionaryService dictionary):ControllerBase
{
 [HttpGet] public Task<KnowledgeSummary>Get(CancellationToken ct)=>overview.GetAsync(OrganizationId(),ct);
 [HttpGet("articles")] public Task<IReadOnlyList<KnowledgeArticle>>Articles([FromQuery]string? query,[FromQuery]string? status,[FromQuery]Guid? categoryId,CancellationToken ct)=>articles.ListAsync(OrganizationId(),query,status,categoryId,ct);
 [HttpPost("articles")] public async Task<IActionResult>CreateArticle(CreateArticleRequest request,CancellationToken ct){var x=await articles.CreateAsync(OrganizationId(),request,UserId(),Correlation(),ct);return Created($"/api/v1/knowledge/articles/{x.Id}",x);}
 [HttpPost("articles/{id:guid}/submit-review")] public Task<KnowledgeArticle>Submit(Guid id,CancellationToken ct)=>articles.SubmitReviewAsync(OrganizationId(),id,UserId(),Correlation(),ct);
 [HttpPost("articles/{id:guid}/publish"),Authorize(Roles="admin,admin_valora,super_admin")] public Task<KnowledgeArticle>Publish(Guid id,CancellationToken ct)=>articles.PublishAsync(OrganizationId(),id,UserId(),Correlation(),ct);
 [HttpPost("articles/{id:guid}/archive"),Authorize(Roles="admin,admin_valora,super_admin")] public Task<KnowledgeArticle>Archive(Guid id,CancellationToken ct)=>articles.ArchiveAsync(OrganizationId(),id,UserId(),Correlation(),ct);
 [HttpGet("playbooks")] public Task<IReadOnlyList<Playbook>>Playbooks(CancellationToken ct)=>playbooks.ListAsync(OrganizationId(),ct);
 [HttpPost("playbooks"),Authorize(Roles="admin,admin_valora,super_admin")] public async Task<IActionResult>CreatePlaybook(CreatePlaybookRequest request,CancellationToken ct)=>Created("/api/v1/knowledge/playbooks",await playbooks.CreateAsync(OrganizationId(),request,UserId(),Correlation(),ct));
 [HttpGet("lessons")] public Task<IReadOnlyList<OrganizationalLesson>>Lessons(CancellationToken ct)=>lessons.ListAsync(OrganizationId(),ct);
 [HttpPost("lessons")] public async Task<IActionResult>CreateLesson(CreateLessonRequest request,CancellationToken ct)=>Created("/api/v1/knowledge/lessons",await lessons.CreateAsync(OrganizationId(),request,UserId(),Correlation(),ct));
 [HttpGet("learning-paths")] public Task<IReadOnlyList<LearningPath>>Paths(CancellationToken ct)=>paths.ListAsync(OrganizationId(),ct);
 [HttpPost("learning-paths"),Authorize(Roles="admin,admin_valora,super_admin")] public async Task<IActionResult>CreatePath(CreateLearningPathRequest request,CancellationToken ct)=>Created("/api/v1/knowledge/learning-paths",await paths.CreateAsync(OrganizationId(),request,UserId(),Correlation(),ct));
 [HttpGet("dictionary")] public Task<IReadOnlyList<CognitiveTerm>>Dictionary(CancellationToken ct)=>dictionary.ListAsync(OrganizationId(),ct);
 [HttpPost("dictionary"),Authorize(Roles="admin,admin_valora,super_admin")] public async Task<IActionResult>CreateTerm(CreateCognitiveTermRequest request,CancellationToken ct)=>Created("/api/v1/knowledge/dictionary",await dictionary.CreateAsync(OrganizationId(),request,UserId(),Correlation(),ct));
 private Guid OrganizationId(){var v=Request.Headers["X-Organization-Id"].FirstOrDefault()??User.FindFirstValue("organization_id")??User.FindFirstValue("organizationId");return Guid.TryParse(v,out var id)?id:throw new UnauthorizedAccessException("Selecione uma organização para acessar o Knowledge Center.");}
 private Guid UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw new UnauthorizedAccessException("Sua sessão precisa ser renovada.");
 private string Correlation()=>HttpContext.TraceIdentifier;
}
