namespace Valora.Application.Forms;

public interface IFormAdministrationRepository {
    Task<IReadOnlyList<FormListItemResponse>> ListAsync(Guid organizationId, FormListQuery query, CancellationToken cancellationToken);
    Task<FormDetailResponse?> GetAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken);
    Task<FormDetailResponse> CreateAsync(Guid organizationId, Guid userId, CreateFormRequest request, CancellationToken cancellationToken);
    Task<FormDetailResponse?> UpdateAsync(Guid organizationId, Guid formId, UpdateFormRequest request, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(Guid organizationId, Guid formId, ArchiveFormRequest request, CancellationToken cancellationToken);
    Task<FormVersionResponse?> PublishVersionAsync(Guid organizationId, Guid formId, Guid userId, PublishFormVersionRequest request, CancellationToken cancellationToken);
    Task<ReorderFormItemResponse?> ReorderAsync(Guid organizationId, Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken);
    Task<FormSectionResponse?> CreateSectionAsync(Guid organizationId, Guid formId, Guid userId, CreateFormSectionRequest request, CancellationToken cancellationToken);
    Task<FormSectionResponse?> UpdateSectionAsync(Guid organizationId, Guid formId, Guid sectionId, Guid userId, UpdateFormSectionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteSectionAsync(Guid organizationId, Guid formId, Guid sectionId, Guid userId, DeleteFormSectionRequest request, CancellationToken cancellationToken);
    Task<QuestionResponse?> CreateQuestionAsync(Guid organizationId, Guid formId, Guid userId, CreateQuestionRequest request, CancellationToken cancellationToken);
    Task<QuestionResponse?> UpdateQuestionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId, UpdateQuestionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteQuestionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId, DeleteQuestionRequest request, CancellationToken cancellationToken);
    Task<QuestionOptionResponse?> CreateOptionAsync(Guid organizationId, Guid formId, Guid questionId, Guid userId, CreateQuestionOptionRequest request, CancellationToken cancellationToken);
    Task<QuestionOptionResponse?> UpdateOptionAsync(Guid organizationId, Guid formId, Guid optionId, Guid userId, UpdateQuestionOptionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteOptionAsync(Guid organizationId, Guid formId, Guid optionId, Guid userId, DeleteQuestionOptionRequest request, CancellationToken cancellationToken);
    Task<FormDetailResponse?> DuplicateAsync(Guid organizationId, Guid formId, Guid userId, DuplicateFormRequest request, CancellationToken cancellationToken);
    Task<FormDetailResponse?> CreateDraftVersionAsync(Guid organizationId, Guid formId, Guid userId, CreateFormVersionRequest request, CancellationToken cancellationToken);
    Task<FormDetailResponse?> PreviewAsync(Guid organizationId, Guid formId, CancellationToken cancellationToken);
}
