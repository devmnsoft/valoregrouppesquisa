using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;

namespace Valora.Application.Services;

public sealed partial class OrganizationBrandingService(IOrganizationBrandingRepository repository) : IOrganizationBrandingService
{
    public Task<OrganizationBrandingResponse> GetAsync(Guid organizationId, CancellationToken cancellationToken = default) => repository.GetAsync(RequireTenant(organizationId), cancellationToken);

    public async Task<OrganizationBrandingResponse> UpdateAsync(Guid organizationId, UpdateOrganizationBrandingRequest request, CancellationToken cancellationToken = default)
    {
        RequireTenant(organizationId);
        var slug = SlugRegex().Replace(request.PublicSlug.Trim().ToLowerInvariant().Normalize(), "-").Trim('-');
        if (!HexRegex().IsMatch(request.PrimaryColor) || !HexRegex().IsMatch(request.SecondaryColor)) throw new ValidationAppException("As cores devem usar o formato hexadecimal #RRGGBB.");
        if (Contrast(request.PrimaryColor, request.SecondaryColor) < 3m) throw new ValidationAppException("As cores não possuem contraste mínimo suficiente.");
        if (slug.Length is < 3 or > 80) throw new ValidationAppException("O slug deve possuir entre 3 e 80 caracteres.");
        if (!IsSafeLogo(request.LogoUrl)) throw new ValidationAppException("A URL do logotipo deve usar HTTPS ou apontar para um asset interno.");
        if (request.WhiteLabelEnabled && !await repository.HasCapabilityAsync(organizationId, "whiteLabel", cancellationToken))
            throw new BusinessRuleAppException("CAPABILITY_NOT_AVAILABLE: O plano contratado não inclui white label.");
        var normalized = request with { PublicSlug = slug, PrimaryColor = request.PrimaryColor.ToUpperInvariant(), SecondaryColor = request.SecondaryColor.ToUpperInvariant() };
        return await repository.UpdateAsync(organizationId, normalized, cancellationToken) ?? throw new ConcurrencyConflictException("A identidade visual foi atualizada por outra sessão ou o slug já está em uso.");
    }

    public async Task<OrganizationSubscriptionResponse> GetSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await repository.GetSubscriptionAsync(RequireTenant(organizationId), cancellationToken) ?? throw new NotFoundAppException("Assinatura não encontrada.");
    public Task<IReadOnlyList<OnboardingStepResponse>> GetOnboardingAsync(Guid organizationId, CancellationToken cancellationToken = default) => repository.GetOnboardingAsync(RequireTenant(organizationId), cancellationToken);
    public async Task CompleteStepAsync(Guid organizationId, string stepCode, CancellationToken cancellationToken = default)
    {
        if (!ManualSteps.Contains(stepCode) || !await repository.CompleteStepAsync(RequireTenant(organizationId), stepCode, cancellationToken)) throw new ValidationAppException("Passo de onboarding inválido ou automático.");
    }
    private static readonly HashSet<string> ManualSteps = ["company_profile", "branding"];
    private static Guid RequireTenant(Guid id) => id == Guid.Empty
        ? throw new ForbiddenAppException("Selecione uma organização para continuar.")
        : id;
    private static bool IsSafeLogo(string? value) => string.IsNullOrWhiteSpace(value) || value.StartsWith("/", StringComparison.Ordinal) || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    private static decimal Contrast(string a, string b) { static decimal L(string c) { var r=Convert.ToInt32(c[1..3],16)/255m;var g=Convert.ToInt32(c[3..5],16)/255m;var b=Convert.ToInt32(c[5..7],16)/255m;return .2126m*r+.7152m*g+.0722m*b;} var x=L(a);var y=L(b);return (Math.Max(x,y)+.05m)/(Math.Min(x,y)+.05m); }
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex HexRegex();
    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)] private static partial Regex SlugRegex();
}
