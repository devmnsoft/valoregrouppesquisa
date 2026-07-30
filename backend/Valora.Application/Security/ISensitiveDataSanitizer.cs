namespace Valora.Application.Security;

public interface ISensitiveDataSanitizer
{
    string MaskEmail(string? email);

    string Hash(string? value);
}
