using Microsoft.Extensions.Options;
using Valora.Application.Communication;

namespace Valora.Application.CommercialDelivery;

public sealed class DiagnosticCampaignService(IDiagnosticCampaignRepository repository, IOptions<EmailOptions> emailOptions) : IDiagnosticCampaignService
{
    public Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct) => repository.GetAsync(organizationId, surveyId, ct);

    public Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId, CreateCampaignRequest request, string correlationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 180) throw new ArgumentException("Informe um nome de campanha válido.");
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 10_000) throw new ArgumentException("Informe uma mensagem de convite válida.");
        var channel = request.Channel.Trim().ToLowerInvariant();
        if (channel is not ("manual" or "whatsapp_manual" or "email")) throw new ArgumentException("Selecione um canal de campanha válido.");
        if (channel == "email" && !EmailConfigurationValidator.Validate(emailOptions.Value).CanSend)
            throw new InvalidOperationException("Envio de e-mail ainda não configurado neste ambiente. O link público está disponível para compartilhamento manual.");
        return repository.CreateAsync(organizationId, surveyId, userId, request, correlationId, ct);
    }

    public Task<CampaignCommandResult?> SendAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct)
    {
        var configuration = EmailConfigurationValidator.Validate(emailOptions.Value);
        return repository.SendAsync(organizationId, surveyId, userId, configuration.CanSend, correlationId, ct);
    }

    public Task<CampaignCommandResult?> CancelAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct) =>
        repository.CancelAsync(organizationId, surveyId, userId, correlationId, ct);
}
