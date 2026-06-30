using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IOrganizationRepository { Task<dynamic?> GetAsync(Guid id); Task<Guid> CreateAsync(string name,string email,string slug,string planId); Task UpdateCurrentAsync(Guid id, UpdateOrganizationRequest request); Task<int> CountManagersAsync(Guid organizationId); Task<IReadOnlyList<dynamic>> GetSettingsAsync(Guid organizationId); Task UpsertSettingsAsync(Guid organizationId, IReadOnlyDictionary<string,object?> settings); Task<IReadOnlyList<dynamic>> GetUsageAsync(Guid organizationId); }
