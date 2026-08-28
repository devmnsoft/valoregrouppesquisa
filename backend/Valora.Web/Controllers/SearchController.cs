using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Valora.Web.Models.ViewModels;
namespace Valora.Web.Controllers;
[Authorize] public sealed class SearchController:Controller { [HttpGet("Search")] public IActionResult Index()=>View("~/Views/Workspace/Index.cshtml",new ExecutiveWorkspaceViewModel("search","Busca Global","Encontre diagnósticos, ações, decisões, evidências e entregáveis no seu contexto autorizado.")); }
