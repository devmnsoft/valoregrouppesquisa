using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class EmailSenderService(IEmailOperationalRepository repo, IEmailSender sender) : IEmailSenderService
{
    public async Task<int> ProcessAsync(int batchSize = 20)
    {
        var jobs = (await repo.ListJobsAsync(null, "queued")).Take(batchSize).ToList();

        foreach (var job in jobs)
        {
            await repo.MarkProcessingAsync(job.Id);
            await repo.MarkFailedAsync(
                job.Id,
                "Destinatário indisponível no contrato legado; job movido para retry seguro.",
                deadLetter: false);
        }

        return jobs.Count;
    }
}
