using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Advisor;

public sealed record AdvisorConversationDto(Guid Id, string Objective, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record AdvisorMessageDto(Guid Id, Guid ConversationId, string Role, string Content, string Confidence, string[] Limitations, DateTimeOffset CreatedAt, IReadOnlyList<AdvisorEvidenceDto> Evidence);
public sealed record AdvisorEvidenceDto(Guid Id, string SourceType, Guid SourceId, string Title, string Excerpt, string Strength);
public sealed record AdvisorConversationDetailDto(AdvisorConversationDto Conversation, IReadOnlyList<AdvisorMessageDto> Messages);
public sealed record AdvisorTemplateDto(Guid Id, string Code, string Name, string Area, string Status, int Version, bool HumanReviewed, string Content);
public sealed record AdvisorContextOptionDto(string SourceType, Guid SourceId, string Title, string Summary);
public sealed record AdvisorSuggestionDto(Guid Id, string SuggestionType, string Title, string Description, string Status, bool RequiresConfirmation);

public sealed class CreateAdvisorConversationRequest
{
    [Required, StringLength(300, MinimumLength = 5)] public string Objective { get; init; } = "";
    public IReadOnlyList<AdvisorContextSelection> Context { get; init; } = [];
}
public sealed record AdvisorContextSelection([property: Required] string SourceType, Guid SourceId);
public sealed class SendAdvisorMessageRequest
{
    [Required, StringLength(4000, MinimumLength = 3)] public string Content { get; init; } = "";
    [MinLength(1, ErrorMessage = "Selecione ao menos uma evidência para uma análise.")] public IReadOnlyList<AdvisorContextSelection> Context { get; init; } = [];
}
public sealed class CreateAdvisorTemplateRequest
{
    [Required, StringLength(80)] public string Code { get; init; } = "";
    [Required, StringLength(160)] public string Name { get; init; } = "";
    [Required, StringLength(80)] public string Area { get; init; } = "";
    [Required, StringLength(12000), MinLength(30)] public string Content { get; init; } = "";
}
public sealed class AdvisorFeedbackRequest
{
    public bool Useful { get; init; }
    [StringLength(500)] public string? Reason { get; init; }
    [StringLength(1000)] public string? Improvement { get; init; }
}
public sealed record AdvisorModelRequest(string SystemInstruction, string Question, IReadOnlyList<AdvisorContextOptionDto> Evidence);
public sealed record AdvisorModelResult(bool ProviderUsed, string Content, string? Limitation);

public interface IAdvisorConversationRepository
{
    Task<IReadOnlyList<AdvisorConversationDto>> List(Guid organizationId, Guid userId, CancellationToken ct);
    Task<AdvisorConversationDetailDto?> Get(Guid organizationId, Guid userId, Guid id, CancellationToken ct);
    Task<Guid> Create(Guid organizationId, Guid userId, CreateAdvisorConversationRequest request, CancellationToken ct);
}
public interface IAdvisorMessageRepository
{
    Task<Guid> AddUserMessage(Guid organizationId, Guid userId, Guid conversationId, string content, CancellationToken ct);
    Task<Guid> AddResponse(Guid organizationId, Guid conversationId, string content, string confidence, string[] limitations, IReadOnlyList<AdvisorContextOptionDto> evidence, CancellationToken ct);
}
public interface IAdvisorContextBundleRepository { Task<IReadOnlyList<AdvisorContextOptionDto>> Options(Guid organizationId, CancellationToken ct); Task<IReadOnlyList<AdvisorContextOptionDto>> Resolve(Guid organizationId, IReadOnlyList<AdvisorContextSelection> selections, CancellationToken ct); }
public interface IAdvisorPromptTemplateRepository { Task<IReadOnlyList<AdvisorTemplateDto>> List(Guid organizationId, CancellationToken ct); Task<Guid> Create(Guid organizationId, Guid userId, CreateAdvisorTemplateRequest request, CancellationToken ct); }
public interface IAdvisorGuardrailRepository { Task Record(Guid organizationId, Guid userId, Guid? conversationId, string rule, string reason, CancellationToken ct); }
public interface IAdvisorFeedbackRepository { Task Create(Guid organizationId, Guid userId, Guid messageId, AdvisorFeedbackRequest request, CancellationToken ct); }
public interface IAdvisorUsageRepository { Task Record(Guid organizationId, Guid userId, string eventName, Guid? entityId, CancellationToken ct); }
public interface IAdvisorModelProvider { Task<AdvisorModelResult> Generate(AdvisorModelRequest request, CancellationToken ct); }
