using System.Security.Cryptography;
using System.Text;

namespace Valora.Application.Security;

public sealed class SensitiveDataSanitizer : ISensitiveDataSanitizer
{
    private const string MaskedValue = "***";

    public string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return MaskedValue;
        }

        var normalizedEmail = email.Trim();
        var separatorIndex = normalizedEmail.IndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == normalizedEmail.Length - 1)
        {
            return MaskedValue;
        }

        return $"{normalizedEmail[0]}{MaskedValue}@{normalizedEmail[(separatorIndex + 1)..]}";
    }

    public string Hash(string? value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
