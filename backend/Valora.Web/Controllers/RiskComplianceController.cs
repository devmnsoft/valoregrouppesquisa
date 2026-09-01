using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.RiskCompliance;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class RiskComplianceController(ICurrentOrganizationProvider current,RiskRegisterService risks,RiskControlService controls,
    ComplianceFrameworkService frameworks,NonConformityService nonConformities,RiskHeatmapService heatmap):Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct){var o=Scope();if(o is null)return View("NoOrganization");return View(new RiskComplianceIndexViewModel(await risks.Dashboard(o.Value,ct),(await risks.List(o.Value,ct)).Take(6).ToList(),(await controls.List(o.Value,ct)).Take(5).ToList(),(await nonConformities.List(o.Value,ct)).Take(5).ToList()));}
    [HttpGet] public Task<IActionResult> Risks(CancellationToken ct)=>Section("Registro de riscos","RISCO ORGANIZACIONAL","Riscos identificados, avaliados e priorizados sem confundir hipótese com evidência.",ct,"risks");
    [HttpGet] public Task<IActionResult> Heatmap(CancellationToken ct)=>Section("Mapa de riscos","PROBABILIDADE × IMPACTO","Uma leitura visual de criticidade e confiança para apoiar — nunca substituir — a decisão humana.",ct,"heatmap");
    [HttpGet] public Task<IActionResult> Controls(CancellationToken ct)=>Section("Controles internos","EFETIVIDADE COMPROVADA","Desenho, implementação, testes e evidências dos controles vinculados.",ct,"controls");
    [HttpGet] public Task<IActionResult> Compliance(CancellationToken ct)=>Section("Conformidade","ADERÊNCIA COM EVIDÊNCIA","Frameworks e requisitos; ausência de evidência nunca é apresentada como conformidade.",ct,"frameworks");
    [HttpGet] public Task<IActionResult> NonConformities(CancellationToken ct)=>Section("Não conformidades","CORREÇÃO RASTREÁVEL","Causas, impactos e vínculo obrigatório com mitigação.",ct,"nonconformities");
    [HttpGet] public Task<IActionResult> MitigationPlans(CancellationToken ct)=>Section("Planos de mitigação","RESPONSABILIDADE E PRAZO","Prioridades, responsáveis, ações e evidência antes da conclusão.",ct,"mitigation");
    [HttpGet] public Task<IActionResult> Audits(CancellationToken ct)=>Section("Revisões de auditoria","HISTÓRICO AUDITÁVEL","Revisões, achados e recomendações preservando contexto e autoria.",ct,"audits");
    private async Task<IActionResult> Section(string title,string eyebrow,string description,CancellationToken ct,string kind){var o=Scope();if(o is null)return View("NoOrganization");return View("Section",new RiskComplianceSectionViewModel(title,eyebrow,description,kind=="risks"?await risks.List(o.Value,ct):[],kind=="controls"?await controls.List(o.Value,ct):[],kind=="frameworks"?await frameworks.List(o.Value,ct):[],kind=="nonconformities"?await nonConformities.List(o.Value,ct):[],kind=="heatmap"?await heatmap.Get(o.Value,ct):[]));}
    private Guid? Scope(){var scope=current.GetCurrent();if(scope.IsResolved&&scope.OrganizationId!=Guid.Empty)return scope.OrganizationId;TempData["RiskComplianceError"]="Selecione uma organização para acessar riscos e conformidade.";return null;}
}
