using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;
using Valora.Domain.ValueObjects;
using Valora.Application.CompanyRegistration;
using Valora.Application.Exceptions;
using Valora.Application.Access;

namespace Valora.Application.Services;

public sealed class AuthService(
    IOrganizationRepository organizations,
    IUserRepository users,
    IPlanRepository plans,
    ICommunicationRepository communications,
    IAuthenticationSessionService authenticationSessions,
    IPasswordHasher hasher,
    IPasswordPolicy passwordPolicy,
    AuditService audit,
    IOptions<AuthenticationOptions> authenticationOptions,
    ILogger<AuthService> logger,
    RegisterCompanyHandler companyRegistration,
    IAccessAdministrationService? accessAdministration = null)
{
    public async Task<AuthenticationResult> RegisterCompanyAsync(RegisterCompanyRequest request)
    {
        var passwordValidation = passwordPolicy.Validate(request.Password, request.AdministratorEmail, request.CompanyName);
        if (!passwordValidation.IsValid) throw new ArgumentException(string.Join(" ", passwordValidation.Errors));

        // The handler commits every company write before any session or token exists.
        var registration = await companyRegistration.HandleAsync(request);
        var tokens = await authenticationSessions.CreateAsync(registration.UserId, registration.OrganizationId,
            request.AdministratorEmail, "empresa_admin", request.Language);
        var selectedPlanCode = request.PlanCode is "start" ? "start" : "growth";
        var selectedPlan = await plans.GetByIdAsync(selectedPlanCode);
        if (selectedPlan is null)
            throw new ApplicationConfigurationException($"Required trial plan '{selectedPlanCode}' was not found.");
        return CreateAuthenticationResult(tokens,
            new AuthenticatedUserDto(registration.UserId, request.AdministratorName, request.AdministratorEmail, "empresa_admin"),
            new AuthenticatedOrganizationDto(registration.OrganizationId, request.CompanyName, request.TradeName, string.Empty),
            new AuthenticatedPlanDto(selectedPlanCode, selectedPlan.Name),
            await ResolveAccessContextAsync(registration.OrganizationId, registration.UserId, ["empresa_admin"], selectedPlanCode));
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Login started. Email={Email}", LogSanitizer.MaskEmail(request.Email));

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            await RecordRejectedLoginAsync("invalid_request", null, null);
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var maskedEmail = LogSanitizer.MaskEmail(request.Email);
        var user = await users.GetByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Login rejected: user not found. Email={Email}", maskedEmail);
            await RecordRejectedLoginAsync("invalid_credentials", null, null);
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        if (user.DeletedAt is not null)
        {
            logger.LogWarning("Login rejected: user deleted. Email={Email}", maskedEmail);
            await RecordRejectedLoginAsync("invalid_credentials", user.OrganizationId, user.Id);
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Login rejected: user inactive. Email={Email} Status={Status}", maskedEmail, user.Status);
            await RecordAuthenticationEventSafelyAsync(user.OrganizationId, user.Id, "auth.login_failed", "inactive_user");
            throw new InactiveUserException();
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !IsBcryptHash(user.PasswordHash))
        {
            logger.LogWarning("Login rejected: password hash empty or incompatible. Email={Email}", maskedEmail);
            await RecordRejectedLoginAsync("invalid_credentials", user.OrganizationId, user.Id);
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var passwordMatches = VerifyPasswordSafely(request.Password, user.PasswordHash);
        if (passwordMatches is null)
        {
            logger.LogWarning("Login rejected: password hash could not be verified. Email={Email}", maskedEmail);
            await RecordRejectedLoginAsync("invalid_credentials", user.OrganizationId, user.Id);
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        if (!passwordMatches.Value)
        {
            logger.LogWarning("Login rejected: invalid password. Email={Email}", maskedEmail);
            await RecordRejectedLoginAsync("invalid_credentials", user.OrganizationId, user.Id);
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        if (user.RoleCodes.Count == 0)
        {
            logger.LogWarning("Login rejected: role missing. Email={Email}", maskedEmail);
            throw new OrganizationAccessNotConfiguredException();
        }

        if (user.OrganizationId is null || user.OrganizationId == Guid.Empty)
        {
            logger.LogWarning("Login rejected: organization missing. Email={Email}", maskedEmail);
            throw new OrganizationAccessNotConfiguredException();
        }

        var organizationId = user.OrganizationId.Value;

        var organization = await organizations.GetAsync(organizationId);
        if (organization is null)
        {
            logger.LogWarning("Login rejected: active organization missing. Email={Email} OrganizationId={OrganizationId}", maskedEmail, organizationId);
            throw new OrganizationAccessNotConfiguredException();
        }

        var planId = await plans.GetCurrentPlanIdAsync(organizationId);
        if (planId is null)
            logger.LogWarning("Login continuing with safe free fallback: active subscription missing. Email={Email} OrganizationId={OrganizationId}", maskedEmail, organizationId);
        planId ??= "free";
        var currentPlan = await plans.GetByIdAsync(planId);
        if (currentPlan is null)
        {
            logger.LogError("Login failed because the configured plan is unavailable. OrganizationId={OrganizationId} PlanCode={PlanCode}", organizationId, planId);
            throw new ApplicationConfigurationException($"Configured plan '{planId}' was not found or is inactive.");
        }
        var role = user.RoleCodes.FirstOrDefault() ?? "empresa_admin";

        var tokens = await authenticationSessions.CreateAsync(user.Id, organizationId, user.Email, role,
            organization.DefaultLanguageCode);
        await users.TouchLoginAsync(user.Id);
        await audit.LogAsync(new AuditEntry(
            organizationId,
            user.Id,
            "auth.login",
            "user",
            user.Id.ToString(),
            "Login realizado."));
        logger.LogInformation("Login succeeded. UserId={UserId} Email={Email} Role={Role} Plan={Plan}",
            user.Id, maskedEmail, role, planId);
        return CreateAuthenticationResult(tokens,
            new AuthenticatedUserDto(user.Id, user.Name, user.Email, role),
            new AuthenticatedOrganizationDto(organization.Id, organization.Name, organization.PublicName, organization.Slug),
            new AuthenticatedPlanDto(planId, currentPlan.Name),
            await ResolveAccessContextAsync(organizationId, user.Id, user.RoleCodes, planId));
    }

    public async Task<AuthenticationResult> RefreshAsync(RefreshRequest request)
    {
        var tokens = await authenticationSessions.RefreshAsync(request.RefreshToken);
        var user = await users.GetAsync(tokens.UserId) ?? throw new UnauthorizedAccessException("Sessão inválida.");
        if (user.DeletedAt is not null || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase)
            || user.OrganizationId != tokens.OrganizationId)
        {
            await authenticationSessions.LogoutAllAsync(tokens.UserId);
            await RecordAuthenticationEventSafelyAsync(tokens.OrganizationId, tokens.UserId, "auth.session_revoked", "inactive_or_scope_changed");
            throw new UnauthorizedAccessException("Sessão inválida.");
        }
        var organization = await organizations.GetAsync(tokens.OrganizationId);
        if (organization is null) throw new UnauthorizedAccessException("Sessão inválida.");
        var planId = await plans.GetCurrentPlanIdAsync(tokens.OrganizationId) ?? "free";
        var plan = await plans.GetByIdAsync(planId);
        if (plan is null)
            throw new ApplicationConfigurationException($"Configured plan '{planId}' was not found or is inactive.");
        var role = user.RoleCodes.FirstOrDefault() ?? "empresa_admin";
        return CreateAuthenticationResult(tokens,
            new AuthenticatedUserDto(user.Id, user.Name, user.Email, role),
            organization is null ? null : new AuthenticatedOrganizationDto(organization.Id, organization.Name, organization.PublicName, organization.Slug),
            new AuthenticatedPlanDto(planId, plan.Name),
            await ResolveAccessContextAsync(tokens.OrganizationId, user.Id, user.RoleCodes, planId));
    }

    public async Task LogoutAsync(Guid userId, LogoutRequest request)
    {
        await authenticationSessions.LogoutAsync(userId, request.RefreshToken);
        var user = await users.GetAsync(userId);
        await RecordAuthenticationEventSafelyAsync(user?.OrganizationId, userId, "auth.logout", "current_session");
    }

    public async Task LogoutAllAsync(Guid userId)
    {
        await authenticationSessions.LogoutAllAsync(userId);
        var user = await users.GetAsync(userId);
        await RecordAuthenticationEventSafelyAsync(user?.OrganizationId, userId, "auth.logout_all", "all_sessions");
    }
    public Task<IReadOnlyList<SessionDto>> ListSessionsAsync(Guid userId) => authenticationSessions.ListAsync(userId);
    public Task RevokeSessionAsync(Guid userId, Guid sessionId) => authenticationSessions.RevokeAsync(userId, sessionId);


    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress = null, string? userAgent = null)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        logger.LogInformation("Password reset requested. Email={Email}", LogSanitizer.MaskEmail(email));

        var user = string.IsNullOrWhiteSpace(email) ? null : await users.GetByEmailAsync(email);
        if (user is { DeletedAt: null } && string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var tokenHash = HashToken(rawToken);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
            await users.CreatePasswordResetTokenAsync(user.Id, tokenHash, expiresAt, HashNullable(ipAddress), userAgent);
            var resetUrl = $"{authenticationOptions.Value.PasswordResetBaseUrl}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(rawToken)}";
            await communications.AddEmailJobAsync(user.OrganizationId, null, user.Email, "Recuperação de senha - Valora Insight", "password-reset", "pending", JsonSerializer.Serialize(new { userId = user.Id, expiresAt, resetUrl }));
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
        if (user.DeletedAt is not null || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Token inválido ou expirado.");
        var token = await users.GetValidPasswordResetTokenAsync(HashToken(request.Token))
            ?? throw new UnauthorizedAccessException("Token inválido ou expirado.");
        if (token.UserId != user.Id)
        {
            throw new UnauthorizedAccessException("Token inválido ou expirado.");
        }

        await users.UpdatePasswordHashAsync(user.Id, hasher.Hash(request.NewPassword));
        await users.MarkPasswordResetTokenUsedAsync(token.Id);
        await authenticationSessions.LogoutAllAsync(user.Id);
        await audit.LogAsync(new AuditEntry(user.OrganizationId, user.Id, "auth.password_reset_completed", "user", user.Id.ToString(), "Senha redefinida com token válido."));
        logger.LogInformation("Password reset completed. UserId={UserId} Email={Email}", user.Id, LogSanitizer.MaskEmail(user.Email));
    }

    private static void ValidateRegisterRequest(RegisterCompanyRequest request)
    {
        if (!Cnpj.TryCreate(request.Cnpj, out _)
            || string.IsNullOrWhiteSpace(request.AdministratorEmail)
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

    private static bool IsBcryptHash(string hash) =>
        hash.Length == 60 && (hash.StartsWith("$2a$", StringComparison.Ordinal)
            || hash.StartsWith("$2b$", StringComparison.Ordinal)
            || hash.StartsWith("$2y$", StringComparison.Ordinal));

    private bool? VerifyPasswordSafely(string password, string hash)
    {
        try { return hasher.Verify(password, hash); }
        catch (ArgumentException) { return null; }
        catch (FormatException) { return null; }
    }

    private Task RecordRejectedLoginAsync(string reason, Guid? organizationId, Guid? userId)
    {
        return RecordAuthenticationEventSafelyAsync(organizationId, userId, "auth.login_failed", reason);
    }

    private async Task RecordAuthenticationEventSafelyAsync(Guid? organizationId, Guid? userId, string action, string reason)
    {
        try
        {
            await audit.LogAsync(new AuditEntry(organizationId, userId, action, "authentication", userId?.ToString(),
                "Evento de autenticação.", JsonSerializer.Serialize(new { reason })));
        }
        catch (Exception exception)
        {
            // Falha de auditoria nunca revela se a conta existe nem converte uma rejeição em erro 500.
            logger.LogError(exception, "Authentication audit unavailable. Action={Action}", action);
        }
    }

    private static AuthenticationResult CreateAuthenticationResult(
        TokenPair tokens,
        AuthenticatedUserDto user,
        AuthenticatedOrganizationDto? organization,
        AuthenticatedPlanDto? plan,
        AuthenticatedAccessContextDto accessContext)
    {
        return new AuthenticationResult(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            tokens.SessionId,
            user,
            organization,
            plan,
            accessContext);
    }

    private async Task<AuthenticatedAccessContextDto> ResolveAccessContextAsync(Guid organizationId, Guid userId,
        IReadOnlyList<string> roles, string planCode)
    {
        // Platform administration is an explicit policy, not a general authorization bypass.
        // Tenant data still carries the selected organization in scopes and is audited normally.
        if (roles.Contains(ValoraAccessCatalog.PlatformRole, StringComparer.OrdinalIgnoreCase))
        {
            var permissions = ValoraPermissions.All;
            return new(roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), permissions,
                ValoraAccessCatalog.PlatformModules, ResolveCapabilitiesSafely(permissions),
                ["platform", "organization", $"organization:{organizationId}"], "platform", organizationId, planCode);
        }

        if (accessAdministration is null)
        {
            logger.LogError("Authoritative access service is unavailable. OrganizationId={OrganizationId} UserId={UserId}", organizationId, userId);
            return new(roles, [], [], [], [], "missing", organizationId, planCode);
        }

        var effective = await accessAdministration.GetEffectiveAccessAsync(organizationId, userId, CancellationToken.None);
        var scopes = effective.Scopes.SelectMany(scope => new[] { scope.Type, $"{scope.Type}:{scope.Id}" })
            .Append("organization").Append($"organization:{organizationId}")
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var capabilities = ResolveCapabilitiesSafely(effective.GrantedPermissions);
        return new(roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), effective.GrantedPermissions,
            effective.AvailableModules.Select(ValoraAccessCatalog.NormalizeModule).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            capabilities, scopes, "active", organizationId, planCode);
    }

    private IReadOnlyList<string> ResolveCapabilitiesSafely(IEnumerable<string> permissions) =>
        ValoraAccessCatalog.CapabilitiesFor(permissions, permission =>
            logger.LogWarning("Unknown permission ignored while resolving login access. Permission={Permission}", permission));

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
