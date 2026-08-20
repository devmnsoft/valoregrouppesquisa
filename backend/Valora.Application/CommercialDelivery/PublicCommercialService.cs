using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Valora.Application.CommercialDelivery;

public sealed class PublicCommercialService(IPublicCommercialRepository repository)
{
    private static readonly Regex EmailPattern = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> LeadStatuses = ["new", "diagnostic_started", "diagnostic_completed", "contacted", "qualified", "proposal_requested", "converted", "lost", "archived"];

    public Task<PublicLeadCreated> StartAsync(PublicLeadRequest request, string? ip, string? userAgent, CancellationToken ct)
    {
        if (!request.ConsentAccepted) throw new ArgumentException("O consentimento LGPD é obrigatório.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.CompanyName) || !EmailPattern.IsMatch(email))
            throw new ArgumentException("Informe nome, empresa e um e-mail válido.");
        return repository.UpsertAndStartAsync(request with { Email = email }, Hash(email), MaskEmail(email),
            HashOptional(request.Phone), MaskPhone(request.Phone), HashOptional(ip), HashOptional(userAgent), ct);
    }

    public Task<Guid> RequestContactAsync(CommercialRequestInput request, CancellationToken ct)
    {
        if (request.LeadId == Guid.Empty) throw new ArgumentException("Lead inválido.");
        if (request.RequestType is not ("contact" or "plan_interest" or "upgrade")) throw new ArgumentException("Tipo de solicitação inválido.");
        return repository.CreateRequestAsync(request, ct);
    }

    public Task<IReadOnlyList<CommercialLeadItem>> ListAsync(string? status, string? level, string? plan, int page, int pageSize, CancellationToken ct) =>
        repository.ListAsync(status, level, plan, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), ct);
    public Task<CommercialDashboard> DashboardAsync(CancellationToken ct) => repository.DashboardAsync(ct);
    public Task<bool> UpdateStatusAsync(Guid id, string status, Guid? assignedTo, string? reason, CancellationToken ct)
    {
        status = status.Trim().ToLowerInvariant();
        if (!LeadStatuses.Contains(status)) throw new ArgumentException("Status de lead inválido.");
        if (status == "lost" && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Informe o motivo da perda.");
        return repository.UpdateStatusAsync(id, status, assignedTo, reason, ct);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()))).ToLowerInvariant();
    private static string? HashOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Hash(value);
    private static string MaskEmail(string email) { var p = email.Split('@'); return $"{p[0][0]}***@{p[1]}"; }
    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length < 4 ? "***" : $"***{digits[^4..]}";
    }
}
