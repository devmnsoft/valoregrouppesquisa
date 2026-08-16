using Microsoft.Extensions.Options;
using Valora.Api.Configuration;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Api;
public sealed class IntelligenceProcessingWorker(IServiceScopeFactory scopes, IOptions<IntelligenceProcessingOptions> options,
    ILogger<IntelligenceProcessingWorker> logger) : BackgroundService, IIntelligenceProcessingWorker
{
    private readonly string workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) { logger.LogInformation("Valora intelligence worker is disabled by configuration."); return; }
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(2, options.Value.PollIntervalSeconds)));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var jobs = scope.ServiceProvider.GetRequiredService<IIntelligenceProcessingJobService>();
                var repository = scope.ServiceProvider.GetRequiredService<IIntelligenceProcessingJobRepository>();
                var pending = await jobs.GetPendingJobsAsync(Math.Clamp(options.Value.MaxConcurrentJobs, 1, 8), stoppingToken);
                foreach (var job in pending)
                {
                    if (!await repository.LockJobAsync(job.Id, workerId, stoppingToken)) continue;
                    try { await scope.ServiceProvider.GetRequiredService<IIntelligenceProcessingOrchestrator>().ProcessAsync(job, stoppingToken); }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex) { logger.LogError(ex, "Processing job failed safely. JobId={JobId} OrganizationId={OrganizationId} CorrelationId={CorrelationId}",job.Id,job.OrganizationId,job.CorrelationId); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex,"Intelligence processing polling cycle failed; worker will continue."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
