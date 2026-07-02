using Microsoft.AspNetCore.Mvc;
namespace Valora.Web.Controllers;
public sealed class MigrationController : Controller
{
    public IActionResult Index()=>View();
    public IActionResult Batches()=>View();
    [HttpGet("/Migration/Batch/{id:guid}")] public IActionResult Batch(Guid id){ViewData["BatchId"]=id; return View();}
    public IActionResult Upload()=>View();
    [HttpGet("/Migration/DryRun/{batchId:guid}")] public IActionResult DryRun(Guid batchId){ViewData["BatchId"]=batchId; return View();}
    [HttpGet("/Migration/Conflicts/{batchId:guid}")] public IActionResult Conflicts(Guid batchId){ViewData["BatchId"]=batchId; return View();}
    [HttpGet("/Migration/Reconciliation/{batchId:guid}")] public IActionResult Reconciliation(Guid batchId){ViewData["BatchId"]=batchId; return View();}
    [HttpGet("/Migration/Rollback/{batchId:guid}")] public IActionResult Rollback(Guid batchId){ViewData["BatchId"]=batchId; return View();}
    [HttpGet("/Migration/CutoverReadiness/{batchId:guid}")] public IActionResult CutoverReadiness(Guid batchId){ViewData["BatchId"]=batchId; return View();}
}
