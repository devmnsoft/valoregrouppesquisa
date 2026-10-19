using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

public sealed class MigrationController : Controller {
    private readonly IMigrationBatchRepository? _batchRepo;

    [ActivatorUtilitiesConstructor]
    public MigrationController(IMigrationBatchRepository batchRepo) {
        _batchRepo = batchRepo;
    }

    public MigrationController() {
    }

    [HttpGet("/Migration")]
    public async Task<IActionResult> Index([FromQuery] Guid? batchId, CancellationToken ct) {
        ViewData["Title"] = "Migração Controlada";

        if (_batchRepo is null) {
            return View(new MigrationIndexViewModel([], null, 0, 0, 0));
        }

        var batches = await _batchRepo.ListAsync(ct);
        var selected = batchId.HasValue
            ? batches.FirstOrDefault(b => b.Id == batchId.Value)
            : batches.FirstOrDefault();

        int totalBatches = batches.Count;
        int totalValid = batches.Sum(b => b.ValidRecords);
        int totalConflicts = batches.Sum(b => b.ConflictRecords);

        ViewData["BatchId"] = selected?.Id.ToString() ?? "";

        return View(new MigrationIndexViewModel(batches, selected, totalBatches, totalValid, totalConflicts));
    }

    public IActionResult Batches() => View();

    [HttpGet("/Migration/Batch/{id:guid}")]
    public IActionResult Batch(Guid id) {
        ViewData["BatchId"] = id;
        return View();
    }

    public IActionResult Upload() => View();

    [HttpGet("/Migration/DryRun/{batchId:guid}")]
    public IActionResult DryRun(Guid batchId) {
        ViewData["BatchId"] = batchId;
        return View();
    }

    [HttpGet("/Migration/Conflicts/{batchId:guid}")]
    public IActionResult Conflicts(Guid batchId) {
        ViewData["BatchId"] = batchId;
        return View();
    }

    [HttpGet("/Migration/Reconciliation/{batchId:guid}")]
    public IActionResult Reconciliation(Guid batchId) {
        ViewData["BatchId"] = batchId;
        return View();
    }

    [HttpGet("/Migration/Rollback/{batchId:guid}")]
    public IActionResult Rollback(Guid batchId) {
        ViewData["BatchId"] = batchId;
        return View();
    }

    [HttpGet("/Migration/CutoverReadiness/{batchId:guid}")]
    public IActionResult CutoverReadiness(Guid batchId) {
        ViewData["BatchId"] = batchId;
        return View();
    }
}
