namespace Valora.Application.DTOs;

public sealed record ExportRequest(string Entity,string Format = "csv", string? FilterJson = null);
