namespace Valora.Application.Forms;

public interface IFormAdministrationService
{
    Task<IReadOnlyList<FormListItemResponse>> ListAsync(Guid organizationId, FormListQuery query, CancellationToken cancellationToken);
    Task<FormDetailResponse?> GetAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken);
    Task<FormDetailResponse> CreateAsync(Guid organizationId, Guid userId, CreateFormRequest request, CancellationToken cancellationToken);
    Task<FormDetailResponse?> UpdateAsync(Guid organizationId, Guid formId, UpdateFormRequest request, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(Guid organizationId, Guid formId, ArchiveFormRequest request, CancellationToken cancellationToken);
    Task<FormVersionResponse?> PublishAsync(Guid organizationId, Guid formId, Guid userId, PublishFormVersionRequest request, CancellationToken cancellationToken);
    Task<ReorderFormItemResponse?> ReorderAsync(Guid organizationId, Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken);
}
