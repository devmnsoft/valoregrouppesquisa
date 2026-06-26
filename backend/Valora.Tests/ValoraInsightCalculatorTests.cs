using Valora.Application.Results;
using Xunit;
namespace Valora.Tests;
public sealed class ValoraInsightCalculatorTests
{
    [Fact]
    public void Score72Of125ReturnsStructuring()
    {
        var answers = new[]{ new AnswerScore("Cultura e Propósito", 15), new AnswerScore("Gestão e Governança", 15), new AnswerScore("Liderança", 14), new AnswerScore("Pessoas e Talentos", 14), new AnswerScore("Resultados e Crescimento", 14) };
        var result = new ValoraInsightCalculator().Calculate(answers);
        Assert.Equal(72, result.TotalScore);
        Assert.Equal("Em estruturação", result.Level);
    }
}
