using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Application.Contracts;
public interface IOrganizationRepository { Task<OrganizationRecord?> GetAsync(Guid id); Task<Guid> CreateAsync(string name,string email,string slug,string planId); Task UpdateCurrentAsync(Guid id, UpdateOrganizationRequest request); Task<int> CountManagersAsync(Guid organizationId); Task<IReadOnlyList<OrganizationSettingRecord>> GetSettingsAsync(Guid organizationId); Task UpsertSettingsAsync(Guid organizationId, IReadOnlyDictionary<string,object?> settings); Task<IReadOnlyList<OrganizationUsageRecord>> GetUsageAsync(Guid organizationId); }
