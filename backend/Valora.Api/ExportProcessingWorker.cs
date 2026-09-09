using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exports;

namespace Valora.Api;

public sealed class ExportProcessingWorker(IServiceScopeFactory scopes, ILogger<ExportProcessingWorker> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IExportRepository>();
                var reader = scope.ServiceProvider.GetRequiredService<IExportDataReader>();
                var renderer = scope.ServiceProvider.GetRequiredService<ExportDocumentRenderer>();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
                var jobs = await repository.ClaimAsync(workerId, 4, stoppingToken);
                foreach (var job in jobs)
                {
                    try
                    {
                        var data = await reader.ReadAsync(job.OrganizationId, job.Entity, job.FilterJson, stoppingToken);
                        var generated = renderer.Render(job, data, DateTimeOffset.UtcNow);
                        await repository.CompleteAsync(generated, stoppingToken);
                        await audit.AddAsync(new AuditEntry(job.OrganizationId, null, "export.completed", "export",
                            job.Id.ToString(), $"Exportação concluída com {data.Rows.Count} registro(s).", "{}"));
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception exception)
                    {
                        var deadLetter = job.Attempts >= job.MaxAttempts;
                        DateTimeOffset? retryAt = deadLetter ? null : DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, job.Attempts) * 15);
                        await repository.FailAsync(job.Id, "EXPORT_GENERATION_FAILED", retryAt, deadLetter, stoppingToken);
                        logger.LogError(exception, "Export generation failed. JobId={JobId} OrganizationId={OrganizationId} CorrelationId={CorrelationId} Attempts={Attempts}",
                            job.Id, job.OrganizationId, job.CorrelationId, job.Attempts);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Export worker polling cycle failed; the worker will retry.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
