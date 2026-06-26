using Valora.Application.Results;
using Xunit;
namespace Valora.Tests;
public sealed class ResultCalculationTests
{
    [Fact]
    public void Score72ReturnsEmEstruturacao()
    {
        var calc = new ValoraInsightCalculator();
        var result = calc.Calculate(new [] { new AnswerScore("Gestão", 72m) });
        Assert.Equal("Em estruturação", result.Level);
    }
}
