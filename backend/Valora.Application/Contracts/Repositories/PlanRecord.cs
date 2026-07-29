using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public sealed record PlanRecord(Guid Id,string Code,string Name,bool IsPublic,bool IsActive);
