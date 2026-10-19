using Valora.Application.DTOs;

namespace Valora.Web.Models.ViewModels;

public sealed record MigrationIndexViewModel(
    IReadOnlyList<MigrationBatchDto> Batches,
    MigrationBatchDto? SelectedBatch,
    int TotalBatches,
    int TotalValid,
    int TotalConflicts
);
