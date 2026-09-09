using Microsoft.Extensions.Options;
using Valora.Application.Communication;

namespace Valora.Application.CommercialDelivery;

public sealed class DiagnosticCampaignService(
    IDiagnosticCampaignRepository repository,
    IOptions<EmailOptions> emailOptions) : IDiagnosticCampaignService {
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Transitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal) {
            [DiagnosticCampaignStatus.Draft] = Set(DiagnosticCampaignStatus.Scheduled, DiagnosticCampaignStatus.Sending, DiagnosticCampaignStatus.Active, DiagnosticCampaignStatus.Cancelled),
            [DiagnosticCampaignStatus.Scheduled] = Set(DiagnosticCampaignStatus.Sending, DiagnosticCampaignStatus.Active, DiagnosticCampaignStatus.Cancelled),
            [DiagnosticCampaignStatus.Sending] = Set(DiagnosticCampaignStatus.Active, DiagnosticCampaignStatus.Paused, DiagnosticCampaignStatus.Failed, DiagnosticCampaignStatus.Cancelled),
            [DiagnosticCampaignStatus.Active] = Set(DiagnosticCampaignStatus.Paused, DiagnosticCampaignStatus.Closed, DiagnosticCampaignStatus.Failed, DiagnosticCampaignStatus.Cancelled),
            [DiagnosticCampaignStatus.Paused] = Set(DiagnosticCampaignStatus.Active, DiagnosticCampaignStatus.Closed, DiagnosticCampaignStatus.Cancelled),
            [DiagnosticCampaignStatus.Failed] = Set(DiagnosticCampaignStatus.Sending, DiagnosticCampaignStatus.Cancelled),
            [DiagnosticCampaignStatus.Closed] = Set(),
            [DiagnosticCampaignStatus.Cancelled] = Set()
        };

    public Task<IReadOnlyList<DiagnosticCampaignDto>> ListAsync(Guid organizationId, CancellationToken ct) =>
        repository.ListAsync(RequireOrganization(organizationId), ct);

    public Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct) =>
        repository.GetAsync(RequireOrganization(organizationId), surveyId, ct);

    public Task<IReadOnlyList<CampaignHistoryDto>> HistoryAsync(Guid organizationId, Guid surveyId, CancellationToken ct) =>
        repository.HistoryAsync(RequireOrganization(organizationId), surveyId, ct);

    public Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId,
        CreateCampaignRequest request, string correlationId, CancellationToken ct) {
        RequireOrganization(organizationId);
        RequireUser(userId);
        if (surveyId == Guid.Empty) throw new ArgumentException("Selecione um diagnóstico publicado.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 180) throw new ArgumentException("Informe um nome de campanha válido.");
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length > 10_000) throw new ArgumentException("Informe uma mensagem de convite válida.");
        var channel = request.Channel.Trim().ToLowerInvariant();
        if (channel is not ("manual" or "whatsapp_manual" or "email")) throw new ArgumentException("Selecione um canal de campanha válido.");
        if (channel == "email" && string.IsNullOrWhiteSpace(request.Subject)) throw new ArgumentException("Informe o assunto do e-mail.");
        if (request.StartsAt is not null && request.EndsAt is not null && request.EndsAt <= request.StartsAt)
            throw new ArgumentException("A data de encerramento deve ser posterior à data de início.");
        if (request.EndsAt <= DateTimeOffset.UtcNow) throw new ArgumentException("A data de encerramento deve estar no futuro.");
        if (request.TargetParticipationRate is not null and (< 1 or > 100))
            throw new ArgumentException("A meta de adesão deve estar entre 1% e 100%.");
        if (channel == "email" && (request.Recipients is null || request.Recipients.Count == 0))
            throw new ArgumentException("Adicione ao menos um destinatário para o canal de e-mail.");
        if (request.Recipients?.Any(recipient => !recipient.HasConsent && !recipient.HasLegalBasis) == true)
            throw new ArgumentException("Cada destinatário por e-mail exige consentimento ou base legal registrada.");
        return repository.CreateAsync(organizationId, surveyId, userId, request with {
            Name = request.Name.Trim(),
            Message = request.Message.Trim(),
            Channel = channel,
            Subject = request.Subject?.Trim()
        }, correlationId, ct);
    }

    public async Task<CampaignCommandResult?> TransitionAsync(Guid organizationId, Guid surveyId, Guid userId,
        string targetStatus, CampaignTransitionRequest request, string correlationId, CancellationToken ct) {
        RequireOrganization(organizationId);
        RequireUser(userId);
        targetStatus = targetStatus.Trim().ToLowerInvariant();
        if (!DiagnosticCampaignStatus.All.Contains(targetStatus)) throw new ArgumentException("Estado de campanha inválido.");
        var campaign = await repository.GetAsync(organizationId, surveyId, ct);
        if (campaign is null) return null;
        if (!Transitions.TryGetValue(campaign.Status, out var allowed) || !allowed.Contains(targetStatus))
            throw new InvalidOperationException($"A transição de {campaign.Status} para {targetStatus} não é permitida.");
        if (targetStatus == DiagnosticCampaignStatus.Sending && campaign.Channel == "email" &&
            !EmailConfigurationValidator.Validate(emailOptions.Value).CanSend)
            throw new InvalidOperationException("O canal de e-mail ainda não está configurado neste ambiente.");
        if (targetStatus == DiagnosticCampaignStatus.Scheduled && campaign.StartsAt is null)
            throw new InvalidOperationException("Informe a data de início antes de agendar.");
        if (targetStatus == DiagnosticCampaignStatus.Scheduled && campaign.StartsAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("A data de início da campanha agendada deve estar no futuro.");
        if (targetStatus == DiagnosticCampaignStatus.Sending && campaign.RecipientCount == 0 && string.IsNullOrWhiteSpace(campaign.PublicUrl))
            throw new InvalidOperationException("Configure destinatários válidos ou um link público antes do envio.");
        if (targetStatus == DiagnosticCampaignStatus.Closed && campaign.EndsAt < DateTimeOffset.UtcNow && string.IsNullOrWhiteSpace(request.Justification))
            throw new InvalidOperationException("Informe uma justificativa para encerrar uma campanha após a data planejada.");
        return await repository.TransitionAsync(organizationId, surveyId, userId, targetStatus, request, correlationId, ct);
    }

    public Task<CampaignCommandResult?> ResendFailuresAsync(Guid organizationId, Guid surveyId, Guid userId,
        string correlationId, CancellationToken ct) =>
        repository.ResendFailuresAsync(RequireOrganization(organizationId), surveyId, RequireUser(userId), correlationId, ct);

    private static IReadOnlySet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.Ordinal);
    private static Guid RequireOrganization(Guid value) => value != Guid.Empty ? value : throw new UnauthorizedAccessException("Selecione uma organização.");
    private static Guid RequireUser(Guid value) => value != Guid.Empty ? value : throw new UnauthorizedAccessException("Não foi possível identificar o usuário.");
}
