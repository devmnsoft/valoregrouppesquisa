using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IDashboardMetricsRepository { Task<DashboardMetricsDto> GetGlobalAsync(); Task<DashboardMetricsDto> GetOrganizationAsync(Guid organizationId); }
