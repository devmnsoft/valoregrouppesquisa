using Microsoft.Extensions.Options;
using System.Net.Mail;
using Valora.Application.Communication;
using Valora.Application.Contracts;

namespace Valora.Application.CommercialDelivery;

public sealed class DiagnosticCampaignService(
    IDiagnosticCampaignRepository repository,
    IOptions<EmailOptions> emailOptions, ISurveyRepository surveys) : IDiagnosticCampaignService {
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
        var recipients = NormalizeRecipients(request.Recipients);
        if (channel == "email" && recipients.Count == 0)
            throw new ArgumentException("Adicione ao menos um destinatário para o canal de e-mail.");
        if (channel != "email" && recipients.Count > 0)
            throw new ArgumentException("Destinatários por e-mail só devem ser informados no canal de e-mail.");
        if (recipients.Any(recipient => !recipient.HasConsent && !recipient.HasLegalBasis))
            throw new ArgumentException("Cada destinatário por e-mail exige consentimento ou base legal registrada.");
        return repository.CreateAsync(organizationId, surveyId, userId, request with {
            Name = request.Name.Trim(),
            Message = request.Message.Trim(),
            Channel = channel,
            Subject = channel == "email" ? request.Subject?.Trim() : null,
            Recipients = recipients
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
        if (request.ExpectedVersion is { } expected && expected != campaign.Version)
            throw new InvalidOperationException("A campanha mudou. Atualize a situação antes de confirmar novamente.");
        if (!Transitions.TryGetValue(campaign.Status, out var allowed) || !allowed.Contains(targetStatus))
            throw new InvalidOperationException($"A transição de {campaign.Status} para {targetStatus} não é permitida.");
        if (targetStatus == DiagnosticCampaignStatus.Sending && campaign.Channel == "email" &&
            !EmailConfigurationValidator.Validate(emailOptions.Value).CanSend)
            throw new InvalidOperationException("O canal de e-mail ainda não está configurado neste ambiente.");
        if (targetStatus == DiagnosticCampaignStatus.Scheduled && campaign.StartsAt is null)
            throw new InvalidOperationException("Informe a data de início antes de agendar.");
        if (targetStatus == DiagnosticCampaignStatus.Scheduled && campaign.StartsAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("A data de início da campanha agendada deve estar no futuro.");
        if (targetStatus == DiagnosticCampaignStatus.Sending && campaign.Channel != "email" && await surveys.GetPublicLinkAsync(surveyId) is null)
            throw new InvalidOperationException("Gere um link público ativo para compartilhar a campanha manual.");
        if (targetStatus == DiagnosticCampaignStatus.Sending && campaign.Channel == "email" && campaign.RecipientCount == 0)
            throw new InvalidOperationException("Configure destinatários válidos antes de preparar o envio.");
        if (targetStatus == DiagnosticCampaignStatus.Closed && campaign.EndsAt < DateTimeOffset.UtcNow && string.IsNullOrWhiteSpace(request.Justification))
            throw new InvalidOperationException("Informe uma justificativa para encerrar uma campanha após a data planejada.");
        return await repository.TransitionAsync(organizationId, surveyId, userId, targetStatus, request with { ExpectedVersion = request.ExpectedVersion ?? campaign.Version }, correlationId, ct);
    }

    public Task<CampaignCommandResult?> ResendFailuresAsync(Guid organizationId, Guid surveyId, Guid userId,
        string correlationId, CancellationToken ct) =>
        repository.ResendFailuresAsync(RequireOrganization(organizationId), surveyId, RequireUser(userId), correlationId, ct);

    private static IReadOnlySet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.Ordinal);
    private static Guid RequireOrganization(Guid value) => value != Guid.Empty ? value : throw new UnauthorizedAccessException("Selecione uma organização.");
    private static Guid RequireUser(Guid value) => value != Guid.Empty ? value : throw new UnauthorizedAccessException("Não foi possível identificar o usuário.");
    private static IReadOnlyList<CampaignRecipientRequest> NormalizeRecipients(IReadOnlyList<CampaignRecipientRequest>? recipients) {
        if (recipients is null || recipients.Count == 0) return [];
        var normalized = new List<CampaignRecipientRequest>(recipients.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var recipient in recipients) {
            var email = recipient.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !IsEmail(email))
                throw new ArgumentException("Revise os destinatários: há e-mails inválidos na lista.");
            if (!seen.Add(email))
                throw new ArgumentException($"O destinatário {email} aparece mais de uma vez.");
            normalized.Add(recipient with { Email = email });
        }
        return normalized;
    }

    private static bool IsEmail(string value) {
        try { return new MailAddress(value).Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }
}
