namespace Valora.Application.Contracts;
public interface IDashboardMetricsService { Task<Valora.Application.DTOs.DashboardMetricsDto> GetGlobalAsync(); Task<Valora.Application.DTOs.DashboardMetricsDto> GetOrganizationAsync(Guid organizationId); }
