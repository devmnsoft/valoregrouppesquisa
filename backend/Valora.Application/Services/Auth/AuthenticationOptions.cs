using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;

namespace Valora.Application.Services;

public sealed class AuthenticationOptions
{
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
    public string PasswordResetBaseUrl { get; set; } = "https://localhost/Account/ResetPassword";
}
