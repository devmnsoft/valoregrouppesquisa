using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class SubscriptionService(ISubscriptionRepository repo) : ISubscriptionService { public Task<SubscriptionDto?> GetAsync(Guid organizationId)=>repo.GetByOrganizationAsync(organizationId); public Task SetStatusAsync(Guid organizationId,string status)=>repo.SetStatusAsync(organizationId,status); }
