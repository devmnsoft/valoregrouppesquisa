namespace Valora.Application.DTOs;

public record SubmitResponseRequest(string? ParticipantName,string? ParticipantEmail,Dictionary<string,object>? Answers);
