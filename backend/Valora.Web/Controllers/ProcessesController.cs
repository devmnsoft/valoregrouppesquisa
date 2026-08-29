using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Processes;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class ProcessesController(ICurrentOrganizationProvider current,ProcessDefinitionService definitions,
    ProcessInstanceService instances,ProcessSlaService sla,ProcessBottleneckInsightService insights,
    ProcessTemplateService templates):Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct){var o=Scope();if(o is null)return View("NoOrganization");return View(new ProcessesIndexViewModel(await instances.Dashboard(o.Value,ct),await sla.List(o.Value,ct),await insights.List(o.Value,ct)));}
    [HttpGet] public Task<IActionResult> Definitions(CancellationToken ct)=>List("Definições","Processos versionados, com dono e estrutura publicável.",ct,definitions:true);
    [HttpGet] public Task<IActionResult> Builder(Guid? id,CancellationToken ct)=>List("Process Builder","Organize etapas, responsabilidades, evidências, aprovações e SLA antes de publicar.",ct,definitions:true);
    [HttpGet] public Task<IActionResult> Instances(CancellationToken ct)=>List("Instâncias","Execuções reais, responsáveis, origens e prazos.",ct,instances:true);
    [HttpGet] public Task<IActionResult> Approvals(CancellationToken ct)=>List("Aprovações","Decisões humanas preservadas e integralmente rastreáveis.",ct,instances:true);
    [HttpGet] public Task<IActionResult> Sla(CancellationToken ct)=>List("Painel de SLA","Prazos em dia, em risco e atrasados, sem inferir dados ausentes.",ct,sla:true);
    [HttpGet] public Task<IActionResult> Insights(CancellationToken ct)=>List("Gargalos","Leituras baseadas exclusivamente no histórico das execuções.",ct,insights:true);
    [HttpGet] public Task<IActionResult> Templates(CancellationToken ct)=>List("Templates de processos","Modelos adaptáveis, sujeitos à validação e publicação humana.",ct,templates:true);
    private async Task<IActionResult> List(string title,string description,CancellationToken ct,bool definitions=false,bool instances=false,bool sla=false,bool insights=false,bool templates=false){var o=Scope();if(o is null)return View("NoOrganization");return View("List",new ProcessListViewModel(title,description,definitions?await definitionsService().List(o.Value,ct):[],instances?await instancesService().List(o.Value,ct):[],sla?await slaService().List(o.Value,ct):[],insights?await insightsService().List(o.Value,ct):[],templates?await templatesService().List(o.Value,ct):[]));}
    private ProcessDefinitionService definitionsService()=>definitions;
    private ProcessInstanceService instancesService()=>instances;
    private ProcessSlaService slaService()=>sla;
    private ProcessBottleneckInsightService insightsService()=>insights;
    private ProcessTemplateService templatesService()=>templates;
    private Guid? Scope(){var scope=current.GetCurrent();if(scope.IsResolved&&scope.OrganizationId!=Guid.Empty)return scope.OrganizationId;TempData["ProcessError"]="Selecione uma organização para acessar os processos.";return null;}
}
