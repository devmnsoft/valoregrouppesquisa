using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class SubscriptionService(ISubscriptionRepository repo) : ISubscriptionService { public Task<SubscriptionDto?> GetAsync(Guid organizationId)=>repo.GetByOrganizationAsync(organizationId); public Task SetStatusAsync(Guid organizationId,string status)=>repo.SetStatusAsync(organizationId,status); }
