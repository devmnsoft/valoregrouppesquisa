using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public sealed record PlanLimitRecord(string LimitKey,int? LimitValue,string Period);
