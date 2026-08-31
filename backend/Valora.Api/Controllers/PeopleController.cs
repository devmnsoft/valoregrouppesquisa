using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.People;

namespace Valora.Api.Controllers;

[Authorize,ApiController,Route("api/v1/people")]
public sealed class PeopleController(PeopleInsightService insights,PeopleProfileService profiles,PeopleTeamService teams,CultureAssessmentService culture,EngagementSignalService engagement,CompetencyFrameworkService competencies,CompetencyAssessmentService competencyAssessments,DevelopmentPlanService plans,PeopleRiskSignalService risks):ControllerBase
{
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private Guid? OrganizationId=>Guid.TryParse(User.FindFirstValue("organization_id"),out var claim)&&claim!=Guid.Empty?claim:Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(),out var header)&&header!=Guid.Empty?header:null;
    private ActionResult MissingOrganization()=>BadRequest(new{code="ORGANIZATION_REQUIRED",message="Selecione uma organização para acessar o People.",correlationId=HttpContext.TraceIdentifier});
    [HttpGet] public async Task<ActionResult> Dashboard(CancellationToken ct)=>OrganizationId is{}o?Ok(await insights.Get(o,ct)):MissingOrganization();
    [HttpGet("profiles")] public async Task<ActionResult> Profiles(CancellationToken ct)=>OrganizationId is{}o?Ok(await profiles.List(o,ct)):MissingOrganization();
    [HttpPost("profiles")] public async Task<ActionResult> CreateProfile(CreatePeopleProfileRequest request,CancellationToken ct)=>OrganizationId is{}o?Created("/api/v1/people/profiles",new{id=await profiles.Create(o,UserId,request,ct),eventName="people.profile.created"}):MissingOrganization();
    [HttpGet("teams")] public async Task<ActionResult> Teams(CancellationToken ct)=>OrganizationId is{}o?Ok(await teams.List(o,ct)):MissingOrganization();
    [HttpPost("teams")] public async Task<ActionResult> CreateTeam(CreatePeopleTeamRequest request,CancellationToken ct)=>OrganizationId is{}o?Created("/api/v1/people/teams",new{id=await teams.Create(o,UserId,request,ct),eventName="people.team.created"}):MissingOrganization();
    [HttpGet("culture")] public async Task<ActionResult> Culture(CancellationToken ct)=>OrganizationId is{}o?Ok(await culture.List(o,ct)):MissingOrganization();
    [HttpPost("culture/assessments")] public async Task<ActionResult> AssessCulture(CreateCultureAssessmentRequest request,CancellationToken ct)=>OrganizationId is{}o?Ok(new{id=await culture.Create(o,UserId,request,ct),eventName="people.culture.assessed"}):MissingOrganization();
    [HttpGet("engagement")] public async Task<ActionResult> Engagement(CancellationToken ct)=>OrganizationId is{}o?Ok(await engagement.List(o,ct)):MissingOrganization();
    [HttpPost("engagement/signals")] public async Task<ActionResult> Signal(CreateEngagementSignalRequest request,CancellationToken ct)=>OrganizationId is{}o?Ok(new{id=await engagement.Create(o,UserId,request,ct),eventName="people.engagement.signal_registered"}):MissingOrganization();
    [HttpGet("competencies")] public async Task<ActionResult> Competencies(CancellationToken ct)=>OrganizationId is{}o?Ok(await competencies.List(o,ct)):MissingOrganization();
    [HttpPost("competencies/assessments")] public async Task<ActionResult> AssessCompetency(CreateCompetencyAssessmentRequest request,CancellationToken ct)=>OrganizationId is{}o?Ok(new{id=await competencyAssessments.Assess(o,UserId,request,ct),eventName="people.competency.assessed"}):MissingOrganization();
    [HttpGet("development-plans")] public async Task<ActionResult> Plans(CancellationToken ct)=>OrganizationId is{}o?Ok(await plans.List(o,ct)):MissingOrganization();
    [HttpPost("development-plans")] public async Task<ActionResult> CreatePlan(CreateDevelopmentPlanRequest request,CancellationToken ct)=>OrganizationId is{}o?Ok(new{id=await plans.Create(o,UserId,request,ct),eventName="people.development_plan.created"}):MissingOrganization();
    [HttpGet("risks")] public async Task<ActionResult> Risks(CancellationToken ct)=>OrganizationId is{}o?Ok(await risks.List(o,ct)):MissingOrganization();
    [HttpPost("risks/review")] public async Task<ActionResult> ReviewRisk(ReviewPeopleRiskRequest request,CancellationToken ct){if(OrganizationId is not{}o)return MissingOrganization();await risks.Review(o,UserId,request,ct);return Ok(new{eventName="people.risk_signal.reviewed",message="Revisão humana registrada. O sinal não representa uma decisão automática de RH."});}
}
