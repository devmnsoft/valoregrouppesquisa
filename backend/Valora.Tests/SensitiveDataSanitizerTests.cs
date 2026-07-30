using Valora.Application.Security;

namespace Valora.Tests;

public sealed class SensitiveDataSanitizerTests
{
    private readonly SensitiveDataSanitizer _sanitizer = new();

    [Theory]
    [InlineData(null, "***")]
    [InlineData("", "***")]
    [InlineData("invalid", "***")]
    [InlineData("ana@example.com", "a***@example.com")]
    [InlineData(" BOB@EXAMPLE.COM ", "B***@EXAMPLE.COM")]
    public void MaskEmail_DoesNotExposeTheLocalPart(string? email, string expected)
    {
        Assert.Equal(expected, _sanitizer.MaskEmail(email));
    }

    [Theory]
    [InlineData(null, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("Valora", "7a8589e339a3457a76f1699bef4c6e8e43f7b1e38e6d070df09ea7c2959964c9")]
    public void Hash_ReturnsDeterministicLowercaseSha256(string? value, string expected)
    {
        Assert.Equal(expected, _sanitizer.Hash(value));
        Assert.Equal(expected, _sanitizer.Hash(value));
    }
}
