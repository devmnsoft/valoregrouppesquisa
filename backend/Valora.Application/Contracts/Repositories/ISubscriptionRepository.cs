namespace Valora.Application.Contracts;
public interface ISubscriptionRepository { Task<Valora.Application.DTOs.SubscriptionDto?> GetByOrganizationAsync(Guid organizationId); Task UpsertAsync(Valora.Application.DTOs.SubscriptionDto subscription); Task SetStatusAsync(Guid organizationId,string status); }
