namespace Valora.Application.DTOs;

public record PlanDto(string Id,string Name,string? Badge,string PriceLabel,string? PriceComplement,int DisplayOrder,Dictionary<string,int> Limits,Dictionary<string,string> Capabilities);
