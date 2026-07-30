using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class DashboardMetricsService(IDashboardMetricsRepository repo) : IDashboardMetricsService { public Task<DashboardMetricsDto> GetGlobalAsync()=>repo.GetGlobalAsync(); public Task<DashboardMetricsDto> GetOrganizationAsync(Guid organizationId)=>repo.GetOrganizationAsync(organizationId); }
