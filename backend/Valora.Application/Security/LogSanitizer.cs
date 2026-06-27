using System.Text.RegularExpressions;

namespace Valora.Application.Security;

public static class LogSanitizer
{
    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email;
        var parts = email.Split('@', 2);
        return parts.Length == 2 && parts[0].Length > 0 ? $"{parts[0][0]}***@{parts[1]}" : "***";
    }

    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;
        var digits = Regex.Replace(phone, "\\D", "");
        return digits.Length <= 4 ? "****" : $"********{digits[^4..]}";
    }

    public static string? MaskDocument(string? document) => string.IsNullOrWhiteSpace(document) ? document : "***.***.***-**";

    public static string? MaskToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return token;
        return token.Length <= 8 ? "***" : $"{token[..4]}***{token[^4..]}";
    }

    public static string? MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
        return Regex.Replace(connectionString, "(?i)(Password|Pwd|User ID|Username|User)\\s*=\\s*[^;]*", "$1=***");
    }

    public static string SanitizeError(Exception ex) => ex.GetType().Name;
}
