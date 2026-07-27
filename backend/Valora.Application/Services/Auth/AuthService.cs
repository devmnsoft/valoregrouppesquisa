using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;
using Valora.Domain.ValueObjects;

namespace Valora.Application.Services;

public sealed class AuthService(
    IOrganizationRepository organizations,
    IUserRepository users,
    IPlanRepository plans,
    ICommunicationRepository communications,
    IJwtTokenService jwt,
    IPasswordHasher hasher,
    IPasswordPolicy passwordPolicy,
    AuditService audit,
    ILogger<AuthService> logger)
{
    public async Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request)
    {
        ValidateRegisterRequest(request);
        var passwordValidation = passwordPolicy.Validate(request.Password, request.AdministratorEmail, request.CompanyName);
        if (!passwordValidation.IsValid)
        {
            throw new ArgumentException(string.Join(" ", passwordValidation.Errors));
        }
        logger.LogInformation("Company registration started. Email={Email}", LogSanitizer.MaskEmail(request.AdministratorEmail));

        var existing = await users.GetByEmailAsync(request.AdministratorEmail);
        if (existing is not null)
        {
            logger.LogWarning("Company registration conflict. Email={Email}", LogSanitizer.MaskEmail(request.AdministratorEmail));
            throw new InvalidOperationException("E-mail já cadastrado.");
        }

        var organizationId = await organizations.CreateAsync(
            request.CompanyName,
            request.AdministratorEmail,
            BuildSlug(request.CompanyName),
            "free");

        await plans.CreateSubscriptionAsync(organizationId, "free");

        var userId = await users.CreateAsync(
            organizationId,
            request.AdministratorName,
            request.AdministratorEmail,
            hasher.Hash(request.Password),
            "empresa_admin");

        await audit.LogAsync(new AuditEntry(
            organizationId,
            userId,
            "auth.register_company",
            "organization",
            organizationId.ToString(),
            "Empresa cadastrada via API."));

        logger.LogInformation("Company registration succeeded. OrganizationId={OrganizationId} UserId={UserId} Email={Email}", organizationId, userId, LogSanitizer.MaskEmail(request.AdministratorEmail));

        return new AuthResponse(
            jwt.CreateToken(userId, organizationId, request.AdministratorEmail, "empresa_admin"),
            new { id = userId, name = request.AdministratorName, email = request.AdministratorEmail, role = "empresa_admin" },
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

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        await users.TouchLoginAsync(user.Id);
        logger.LogInformation("Login succeeded. UserId={UserId} Email={Email}", user.Id, LogSanitizer.MaskEmail(user.Email));

        await audit.LogAsync(new AuditEntry(
            user.OrganizationId,
            user.Id,
            "auth.login",
            "user",
            user.Id.ToString(),
            "Login realizado."));

        var organization = await organizations.GetAsync(user.OrganizationId);

        var plan = await plans.GetByIdAsync(await plans.GetCurrentPlanIdAsync(user.OrganizationId) ?? "free");

        return new AuthResponse(
            jwt.CreateToken(user.Id, user.OrganizationId, user.Email, user.RoleCodes.FirstOrDefault() ?? "empresa_admin"),
            new { id = user.Id, name = user.Name, email = user.Email, role = user.RoleCodes.FirstOrDefault() },
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
            await users.CreatePasswordResetTokenAsync(user.Id, tokenHash, expiresAt, HashNullable(ipAddress), userAgent);
            await communications.AddEmailJobAsync(user.OrganizationId, null, user.Email, "Recuperação de senha - Valora Insight", "password-reset", "pending", JsonSerializer.Serialize(new { userId = user.Id, expiresAt, delivery = "password-reset-link-required" }));
            await audit.LogAsync(new AuditEntry(user.OrganizationId, user.Id, "auth.forgot_password_requested", "user", user.Id.ToString(), "Recuperação de senha solicitada."));
        }

        logger.LogInformation("Password reset request accepted. Email={Email}", LogSanitizer.MaskEmail(email));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Solicitação de redefinição inválida.");
        }

        var passwordValidation = passwordPolicy.Validate(request.NewPassword, request.Email);
        if (!passwordValidation.IsValid)
        {
            throw new ArgumentException(string.Join(" ", passwordValidation.Errors));
        }

        var user = await users.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Token inválido ou expirado.");
        var token = await users.GetValidPasswordResetTokenAsync(HashToken(request.Token))
            ?? throw new UnauthorizedAccessException("Token inválido ou expirado.");
        if (token.UserId != user.Id)
        {
            throw new UnauthorizedAccessException("Token inválido ou expirado.");
        }

        await users.UpdatePasswordHashAsync(user.Id, hasher.Hash(request.NewPassword));
        await users.MarkPasswordResetTokenUsedAsync(token.Id);
        await audit.LogAsync(new AuditEntry(user.OrganizationId, user.Id, "auth.password_reset_completed", "user", user.Id.ToString(), "Senha redefinida com token válido."));
        logger.LogInformation("Password reset completed. UserId={UserId} Email={Email}", user.Id, LogSanitizer.MaskEmail(user.Email));
    }

    private static void ValidateRegisterRequest(RegisterCompanyRequest request)
    {
        if (!Cnpj.TryCreate(request.Cnpj, out _)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.CompanyName)
            || string.IsNullOrWhiteSpace(request.AdministratorName)
            || string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.Language)
            || string.IsNullOrWhiteSpace(request.TimeZone)
            || string.IsNullOrWhiteSpace(request.IdempotencyKey)
            || !request.AcceptedTerms
            || !request.AcceptedPrivacyPolicy)
        {
            throw new ArgumentException("Dados de cadastro empresarial inválidos.");
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
