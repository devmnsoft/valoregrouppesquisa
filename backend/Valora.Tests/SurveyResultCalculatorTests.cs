using Valora.Application.Results;
using Xunit;

namespace Valora.Tests;

public sealed class SurveyResultCalculatorTests
{
    [Fact]
    public void Calculates_scale_single_multiple_text_and_dimensions()
    {
        var questions = new[]
        {
            new SurveyQuestionInput("q1", "scale", "Gestão", 1, 5, null, Array.Empty<SurveyOptionInput>()),
            new SurveyQuestionInput("q2", "single", "Gestão", 2, 5, null, new[] { new SurveyOptionInput("a", 1), new SurveyOptionInput("b", 5) }),
            new SurveyQuestionInput("q3", "multiple", "Tecnologia", 1, 0, null, new[] { new SurveyOptionInput("x", 2), new SurveyOptionInput("y", 3) }),
            new SurveyQuestionInput("q4", "text", "Tecnologia", 1, 2, 2, Array.Empty<SurveyOptionInput>())
        };
        var answers = new[]
        {
            new SurveyAnswerInput("q1", 4),
            new SurveyAnswerInput("q2", "b"),
            new SurveyAnswerInput("q3", new[] { "x", "y" }),
            new SurveyAnswerInput("q4", "observação preenchida")
        };

        var result = new SurveyResultCalculator().Calculate(questions, answers);

        Assert.Equal(21, result.RawScore);
        Assert.Equal(22, result.MaxScore);
        Assert.InRange(result.Normalized5, 4.7m, 4.8m);
        Assert.Equal("Alta maturidade", result.Band);
        Assert.Equal(2, result.Dimensions.Count);
    }
}
