using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ISubscriptionService { Task<SubscriptionDto?> GetAsync(Guid organizationId); Task SetStatusAsync(Guid organizationId,string status); }
