using Valora.Application.OrganizationalIntelligence;

namespace Valora.Application.Benchmark;

public sealed class BenchmarkCalculationService(IBenchmarkManagementService service)
{ public Task<BenchmarkComparisonDto> CompareAsync(Guid org, CompareBenchmarkRequest request, CancellationToken ct) => service.CompareAsync(org,request,ct); }
public sealed class BenchmarkQueryService(IBenchmarkManagementService service)
{ public Task<BenchmarkSnapshotDto?> GetAsync(Guid org, Guid id, CancellationToken ct) => service.GetAsync(org,id,ct); }
public sealed class BenchmarkAiInsightService
{ public string Interpret(BenchmarkComparisonDto value) => string.IsNullOrWhiteSpace(value.Limitation) ? "Leitura baseada somente nas diferenças agregadas observadas; não constitui ranking de pessoas." : value.Limitation; }
public sealed class BenchmarkService(IBenchmarkManagementService service)
{ public Task<BenchmarkSnapshotDto> GenerateAsync(Guid org, GenerateBenchmarkRequest request, CancellationToken ct) => service.GenerateAsync(org,request,ct); }
public sealed class GenerateBenchmarkComparisonUseCase(IBenchmarkManagementService service)
{ public Task<BenchmarkComparisonDto> ExecuteAsync(Guid org, CompareBenchmarkRequest request, CancellationToken ct) => service.CompareAsync(org,request,ct); }
public sealed class GetBenchmarkDetailsUseCase(IBenchmarkManagementService service)
{ public Task<BenchmarkSnapshotDto?> ExecuteAsync(Guid org, Guid id, CancellationToken ct) => service.GetAsync(org,id,ct); }
public sealed class GenerateBenchmarkAiInsightUseCase(BenchmarkAiInsightService service)
{ public string Execute(BenchmarkComparisonDto comparison) => service.Interpret(comparison); }
