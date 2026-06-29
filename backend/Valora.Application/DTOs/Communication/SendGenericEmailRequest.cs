namespace Valora.Application.DTOs.Communication;
public sealed record SendGenericEmailRequest(string To, string Subject, string Body, string? ReplyTo);
