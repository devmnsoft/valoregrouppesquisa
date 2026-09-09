using Microsoft.AspNetCore.Authorization;

namespace Valora.Api.Authorization;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;
