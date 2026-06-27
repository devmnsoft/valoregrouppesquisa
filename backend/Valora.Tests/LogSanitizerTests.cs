using Valora.Application.Security;
using Xunit;

namespace Valora.Tests;

public sealed class LogSanitizerTests
{
    [Fact] public void Masks_email() => Assert.Equal("m***@dominio.com", LogSanitizer.MaskEmail("maria@dominio.com"));
    [Fact] public void Masks_phone() => Assert.Equal("********1234", LogSanitizer.MaskPhone("(11) 99999-1234"));
    [Fact] public void Masks_token() => Assert.Equal("abcd***7890", LogSanitizer.MaskToken("abcdef1234567890"));
}
