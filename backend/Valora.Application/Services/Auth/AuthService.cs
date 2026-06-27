using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class AuthService(
    IOrganizationRepository organizations,
    IUserRepository users,
    IPlanRepository plans,
    ICommunicationRepository communications,
    IJwtTokenService jwt,
    IPasswordHasher hasher,
    AuditService audit,
    ILogger<AuthService> logger)
{
    public async Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request)
    {
        ValidateRegisterRequest(request);
        logger.LogInformation("Company registration started. Email={Email}", LogSanitizer.MaskEmail(request.Email));

        var existing = await users.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            logger.LogWarning("Company registration conflict. Email={Email}", LogSanitizer.MaskEmail(request.Email));
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

        logger.LogInformation("Company registration succeeded. OrganizationId={OrganizationId} UserId={UserId} Email={Email}", organizationId, userId, LogSanitizer.MaskEmail(request.Email));

        return new AuthResponse(
            jwt.CreateToken(userId, organizationId, request.Email, "empresa_admin"),
            new { id = userId, name = request.Name, email = request.Email, role = "empresa_admin" },
            new { id = organizationId, name = request.CompanyName },
            await plans.GetByIdAsync("free"));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Login started. Email={Email}", LogSanitizer.MaskEmail(request.Email));

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
        logger.LogInformation("Login succeeded. UserId={UserId} Email={Email}", (Guid)user.id, LogSanitizer.MaskEmail((string)user.email));

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


    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress = null, string? userAgent = null)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        logger.LogInformation("Password reset requested. Email={Email}", LogSanitizer.MaskEmail(email));

        var user = string.IsNullOrWhiteSpace(email) ? null : await users.GetByEmailAsync(email);
        if (user is not null)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var tokenHash = HashToken(rawToken);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
            await users.CreatePasswordResetTokenAsync((Guid)user.id, tokenHash, expiresAt, HashNullable(ipAddress), userAgent);
            await communications.AddEmailJobAsync((Guid?)user.organization_id, null, (string)user.email, "Recuperação de senha - Valora Insight", "password-reset", "pending", JsonSerializer.Serialize(new { userId = user.id, expiresAt, tokenPreview = rawToken[..6] + "...", delivery = "password-reset-link-generated-server-side" }));
            await audit.LogAsync(new AuditEntry((Guid?)user.organization_id, (Guid)user.id, "auth.forgot_password_requested", "user", user.id.ToString(), "Recuperação de senha solicitada."));
        }

        logger.LogInformation("Password reset request accepted. Email={Email}", LogSanitizer.MaskEmail(email));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new ArgumentException("Solicitação de redefinição inválida.");
        }

        var user = await users.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Token inválido ou expirado.");
        var token = await users.GetValidPasswordResetTokenAsync(HashToken(request.Token))
            ?? throw new UnauthorizedAccessException("Token inválido ou expirado.");
        if ((Guid)token.user_id != (Guid)user.id)
        {
            throw new UnauthorizedAccessException("Token inválido ou expirado.");
        }

        await users.UpdatePasswordHashAsync((Guid)user.id, hasher.Hash(request.NewPassword));
        await users.MarkPasswordResetTokenUsedAsync((Guid)token.id);
        await audit.LogAsync(new AuditEntry((Guid?)user.organization_id, (Guid)user.id, "auth.password_reset_completed", "user", user.id.ToString(), "Senha redefinida com token válido."));
        logger.LogInformation("Password reset completed. UserId={UserId} Email={Email}", (Guid)user.id, LogSanitizer.MaskEmail((string)user.email));
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

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? HashNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : HashToken(value);

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
