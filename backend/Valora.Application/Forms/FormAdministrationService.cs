namespace Valora.Application.Forms;

public sealed class FormAdministrationService(IFormAdministrationRepository repository) : IFormAdministrationService
{
    private static readonly HashSet<string> ItemTypes = ["section", "question", "option"];

    public Task<IReadOnlyList<FormListItemResponse>> ListAsync(Guid organizationId, FormListQuery query, CancellationToken cancellationToken) =>
        repository.ListAsync(RequireOrganization(organizationId), query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) }, cancellationToken);

    public Task<FormDetailResponse?> GetAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken) =>
        repository.GetAsync(RequireOrganization(organizationId), formId, cancellationToken);

    public Task<FormDetailResponse> CreateAsync(Guid organizationId, Guid userId, CreateFormRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Informe o nome do formulário.", nameof(request));
        if (request.EstimatedMinutes is < 1 or > 480) throw new ArgumentException("O tempo estimado deve estar entre 1 e 480 minutos.", nameof(request));
        return repository.CreateAsync(RequireOrganization(organizationId), userId, request with { Name = request.Name.Trim() }, cancellationToken);
    }

    public Task<FormDetailResponse?> UpdateAsync(Guid organizationId, Guid formId, UpdateFormRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(RequireOrganization(organizationId), formId, request, cancellationToken);

    public Task<bool> ArchiveAsync(Guid organizationId, Guid formId, ArchiveFormRequest request, CancellationToken cancellationToken) =>
        repository.ArchiveAsync(RequireOrganization(organizationId), formId, request, cancellationToken);

    public Task<FormVersionResponse?> PublishAsync(Guid organizationId, Guid formId, Guid userId, PublishFormVersionRequest request, CancellationToken cancellationToken) =>
        repository.PublishVersionAsync(RequireOrganization(organizationId), formId, RequireUser(userId), request, cancellationToken);

    public Task<ReorderFormItemResponse?> ReorderAsync(Guid organizationId, Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken)
    {
        if (!ItemTypes.Contains(request.ItemType)) throw new ArgumentException("Tipo de item inválido.", nameof(request));
        if (request.NewPosition < 0) throw new ArgumentException("A posição deve ser positiva.", nameof(request));
        return repository.ReorderAsync(RequireOrganization(organizationId), formId, request, cancellationToken);
    }

    private static Guid RequireOrganization(Guid organizationId) => organizationId != Guid.Empty
        ? organizationId
        : throw new UnauthorizedAccessException("O contexto da organização é obrigatório.");

    private static Guid RequireUser(Guid userId) => userId != Guid.Empty
        ? userId
        : throw new UnauthorizedAccessException("Não foi possível identificar o usuário responsável pela operação.");
}
