using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class AuthService(
    IOrganizationRepository organizations,
    IUserRepository users,
    IPlanRepository plans,
    IJwtTokenService jwt,
    IPasswordHasher hasher,
    AuditService audit)
{
    public async Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request)
    {
        ValidateRegisterRequest(request);

        var existing = await users.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new InvalidOperationException("E-mail já cadastrado.");
        }

        var organizationId = await organizations.CreateAsync(
            request.CompanyName,
            request.Email,
            BuildSlug(request.CompanyName),
            "free");

        await plans.CreateSubscriptionAsync(organizationId, "free");

        var userId = await users.CreateAsync(
            organizationId,
            request.Name,
            request.Email,
            hasher.Hash(request.Password),
            "empresa_admin");

        await audit.LogAsync(new AuditEntry(
            organizationId,
            userId,
            "auth.register_company",
            "organization",
            organizationId.ToString(),
            "Empresa cadastrada via API."));

        return new AuthResponse(
            jwt.CreateToken(userId, organizationId, request.Email, "empresa_admin"),
            new { id = userId, name = request.Name, email = request.Email, role = "empresa_admin" },
            new { id = organizationId, name = request.CompanyName },
            await plans.GetByIdAsync("free"));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var user = await users.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (!hasher.Verify(request.Password, (string)user.password_hash))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        await users.TouchLoginAsync((Guid)user.id);

        await audit.LogAsync(new AuditEntry(
            (Guid?)user.organization_id,
            (Guid)user.id,
            "auth.login",
            "user",
            user.id.ToString(),
            "Login realizado."));

        var organization = (Guid?)user.organization_id is Guid organizationId
            ? await organizations.GetAsync(organizationId)
            : null;

        var plan = (Guid?)user.organization_id is Guid planOrganizationId
            ? await plans.GetByIdAsync(await plans.GetCurrentPlanIdAsync(planOrganizationId) ?? "free")
            : null;

        return new AuthResponse(
            jwt.CreateToken((Guid)user.id, (Guid?)user.organization_id, (string)user.email, (string)user.role),
            new { id = user.id, name = user.name, email = user.email, role = user.role },
            organization,
            plan);
    }

    private static void ValidateRegisterRequest(RegisterCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || request.Password.Length < 6
            || string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new ArgumentException("Dados inválidos.");
        }
    }

    private static string BuildSlug(string value)
    {
        var slug = new string(value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        return $"{slug}-{Guid.NewGuid():N}"[..(slug.Length + 7)];
    }
}
