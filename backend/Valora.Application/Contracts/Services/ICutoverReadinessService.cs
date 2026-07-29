using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ICutoverReadinessService { Task<CutoverReadinessDto> GetAsync(Guid batchId,CancellationToken ct=default); }
