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
