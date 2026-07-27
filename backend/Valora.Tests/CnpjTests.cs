using Valora.Domain.ValueObjects;

namespace Valora.Tests;

public sealed class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Create_NormalizesAndValidates(string input)
    {
        var cnpj = Cnpj.Create(input);
        Assert.Equal("11222333000181", cnpj.Value);
        Assert.Equal("11222333", cnpj.Root);
        Assert.Equal("11.222.333/0001-81", cnpj.Formatted);
        Assert.Equal("**.222.333/0001-**", cnpj.Masked);
    }

    [Theory]
    [InlineData("")]
    [InlineData("11.222.333/0001-80")]
    [InlineData("00.000.000/0000-00")]
    [InlineData("123")]
    public void Create_RejectsInvalidValues(string input) => Assert.Throws<ArgumentException>(() => Cnpj.Create(input));

    [Fact]
    public void Equality_UsesNormalizedDigits() => Assert.Equal(Cnpj.Create("11.222.333/0001-81"), Cnpj.Create("11222333000181"));
}
