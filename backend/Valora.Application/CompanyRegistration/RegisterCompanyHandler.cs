using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Domain.ValueObjects;

namespace Valora.Application.CompanyRegistration;

public sealed class RegisterCompanyHandler(IDbTransactionFactory transactions, ICompanyRegistrationRepository registrations,
    IPasswordHasher passwordHasher, RegisterCompanyValidator validator)
{
    public async Task<RegisterCompanyResult> HandleAsync(RegisterCompanyRequest request, string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);
        var normalizedCnpj = new string(request.Cnpj.Where(char.IsDigit).ToArray());
        var canonical = JsonSerializer.Serialize(new { cnpj = normalizedCnpj, companyName = request.CompanyName.Trim(),
            tradeName = request.TradeName?.Trim(), administratorName = request.AdministratorName.Trim(),
            administratorEmail = request.AdministratorEmail.Trim().ToLowerInvariant(), request.Phone, request.Language,
            request.TimeZone, request.AcceptedTerms, request.AcceptedPrivacyPolicy, request.PlanCode, request.RoleTitle });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var ipHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress ?? "unknown")));
        var requestedPlan = request.PlanCode.Trim().ToLowerInvariant();
        var selfServicePlan = requestedPlan is "start" or "growth" ? requestedPlan : "free";
        var trial = selfServicePlan is "start" or "growth";
        await using var unitOfWork = await transactions.BeginAsync(cancellationToken);
        try
        {
            var result = await registrations.RegisterAsync(unitOfWork, new(request.IdempotencyKey, hash, normalizedCnpj,
                request.CompanyName.Trim(), request.TradeName?.Trim(), request.AdministratorName.Trim(),
                request.AdministratorEmail.Trim().ToLowerInvariant(), passwordHasher.Hash(request.Password), request.Phone,
                request.Language, request.TimeZone, ipHash, selfServicePlan, trial ? "trialing" : "active",
                trial ? DateTimeOffset.UtcNow.AddDays(14) : null, request.RoleTitle?.Trim()));
            await unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
