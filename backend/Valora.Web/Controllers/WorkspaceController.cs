using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.ViewModels;
namespace Valora.Web.Controllers;

[Authorize]
public sealed class WorkspaceController : Controller {
    [HttpGet("Workspace")] public IActionResult Index() => View(new ExecutiveWorkspaceViewModel("overview", "Seu trabalho hoje", "Veja o que pede atenção, avance nos diagnósticos e acompanhe resultados e ações sem perder o contexto."));
    [HttpGet("Workspace/MyDay")] public IActionResult MyDay() => View("Index", new ExecutiveWorkspaceViewModel("my-day", "Meu Dia", "Comece pelo que venceu, pelo que é crítico e pelo que depende da sua decisão."));
    [HttpGet("Workspace/Priorities")] public IActionResult Priorities() => View("Index", new ExecutiveWorkspaceViewModel("priorities", "Prioridades executivas", "Acompanhe responsáveis, prazos e progresso sem perder a origem das evidências."));
}
