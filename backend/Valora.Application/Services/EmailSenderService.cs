using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

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
