using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Domain.ValueObjects;

namespace Valora.Application.CompanyRegistration;

public sealed class RegisterCompanyValidator
{
    public void Validate(RegisterCompanyRequest request)
    {
        var plan = request.PlanCode.Trim().ToLowerInvariant();
        if (!Cnpj.TryCreate(request.Cnpj, out _) || string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.AdministratorEmail) ||
            !request.AcceptedTerms || !request.AcceptedPrivacyPolicy || plan is not ("free" or "start" or "growth" or "enterprise"))
            throw new ArgumentException("Dados de cadastro empresarial inválidos.");
    }
}
