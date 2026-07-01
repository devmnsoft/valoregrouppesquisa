namespace Valora.Application.Contracts;
public interface IUsageRepository { Task<Valora.Application.DTOs.UsageDto> GetMonthlyAsync(Guid organizationId,DateTime month); Task RecalculateAsync(Guid organizationId,DateTime month); }
