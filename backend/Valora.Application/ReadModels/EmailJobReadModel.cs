namespace Valora.Application.ReadModels;

public sealed record EmailJobReadModel(
    Guid Id,
    Guid? OrganizationId,
    Guid ResponseId,
    string RecipientEmail,
    string Subject,
    string TemplateKey,
    string Status,
    DateTime CreatedAt);
