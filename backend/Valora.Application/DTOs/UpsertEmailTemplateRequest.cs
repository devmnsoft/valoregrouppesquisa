using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public sealed record UpsertEmailTemplateRequest(
    Guid? OrganizationId,
    [property: Required, StringLength(80, MinimumLength = 2)] string Code,
    [property: Required, StringLength(160, MinimumLength = 2)] string Name,
    [property: Required, StringLength(200, MinimumLength = 2)] string Subject,
    [property: Required, StringLength(100_000, MinimumLength = 3)] string BodyHtml,
    [property: StringLength(100_000)] string? BodyText,
    [property: Required, RegularExpression("^(active|inactive)$")] string Status = "active");
