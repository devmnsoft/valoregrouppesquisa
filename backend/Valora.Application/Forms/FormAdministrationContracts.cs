namespace Valora.Application.Forms;

public sealed record FormListQuery(string? Search = null, string? Status = null, string? Category = null, int Page = 1, int PageSize = 20);
public sealed record FormListItemResponse(Guid Id, string Name, string Description, string Category, int EstimatedMinutes, string Status, int VersionNumber, int Sections, int Questions, int Dimensions, DateTime UpdatedAt, long Version, bool InCurrentUse, bool HasHistoricalUse, bool HasResponses);
public sealed record FormLibraryMetrics(int Drafts, int Published, int Archived, int InCurrentUse);
public sealed record FormListResponse(IReadOnlyList<FormListItemResponse> Items, long Total, int Page, int PageSize,
    int TotalPages, bool HasPreviousPage, bool HasNextPage, IReadOnlyList<string> Categories, FormLibraryMetrics Metrics);
public sealed record FormDetailResponse(Guid Id, Guid OrganizationId, string Name, string? Description, string? Category, int EstimatedMinutes, string Status, Guid? CurrentDraftVersionId, Guid? LatestPublishedVersionId, int SelectedVersionNumber, long Version, long? DraftVersion, IReadOnlyList<FormSectionResponse> Sections);
public sealed record PublicationReviewIssue(string Code, string Message, string? ElementType = null, Guid? ElementId = null);
public sealed record FormPublicationReviewResponse(Guid FormId, Guid VersionId, int VersionNumber, int Sections,
    int RespondableQuestions, int InformationalElements, int MethodologicalLinks,
    IReadOnlyList<PublicationReviewIssue> Blockers, IReadOnlyList<PublicationReviewIssue> Warnings) {
    public bool CanPublish => Blockers.Count == 0;
}
public sealed record FormDimensionCatalogItem(string Code, string Name, bool IsActive, bool IsLegacy);
public sealed record CreateFormRequest(string Name, string? Description, string? Category, int EstimatedMinutes);
public sealed record UpdateFormRequest(string Name, string? Description, string? Category, int EstimatedMinutes, long ExpectedVersion);
public sealed record ArchiveFormRequest(long ExpectedVersion);
public sealed record FormVersionResponse(Guid Id, Guid FormId, int VersionNumber, string Status, int MaximumScore, DateTimeOffset? PublishedAt, long Version);
public sealed record CreateFormVersionRequest(long ExpectedFormVersion);
public sealed record DuplicateFormRequest(string? Name, long ExpectedVersion);
public sealed record PublishFormVersionRequest(long ExpectedVersion);
public sealed record FormSectionResponse(Guid Id, Guid FormVersionId, string Title, string? Description, int Position, long Version, IReadOnlyList<QuestionResponse> Questions);
public sealed record CreateFormSectionRequest(string Title, string? Description, int Position, long ExpectedVersion);
public sealed record UpdateFormSectionRequest(string Title, string? Description, long ExpectedVersion);
public sealed record DeleteFormSectionRequest(long ExpectedVersion);
public sealed record QuestionResponse(Guid Id, Guid SectionId, string Code, string Type, string Title, string? Description, bool Required, string? DimensionCode, decimal Weight, int Position, string Settings, long Version, IReadOnlyList<QuestionOptionResponse> Options);
public sealed record CreateQuestionRequest(Guid SectionId, string Code, string Type, string Title, string? Description, bool Required, string? DimensionCode, decimal Weight, int Position, string? Settings, long ExpectedVersion);
public sealed record UpdateQuestionRequest(string Code, string Type, string Title, string? Description, bool Required, string? DimensionCode, decimal Weight, string? Settings, long ExpectedVersion);
public sealed record DeleteQuestionRequest(long ExpectedVersion);
public sealed record CloneQuestionRequest(Guid TargetSectionId, int Position, long ExpectedVersion);
public sealed record QuestionOptionResponse(Guid Id, Guid QuestionId, string Label, string Value, decimal? Score, int Position, long Version);
public sealed record CreateQuestionOptionRequest(string Label, string Value, decimal? Score, int Position, long ExpectedVersion);
public sealed record UpdateQuestionOptionRequest(string Label, string Value, decimal? Score, long ExpectedVersion);
public sealed record DeleteQuestionOptionRequest(long ExpectedVersion);
public sealed record ReorderFormItemRequest(Guid ItemId, string ItemType, Guid? SourceContainerId, Guid? TargetContainerId, int PreviousPosition, int NewPosition, long ExpectedVersion);
public sealed record ReorderFormItemResponse(Guid ItemId, string ItemType, Guid? ContainerId, int Position, long Version, IReadOnlyList<Guid> FinalOrder);
