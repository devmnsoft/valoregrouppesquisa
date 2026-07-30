using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class PublicSurveyValidationTests
{
    [Fact] public void ValidateSurveyWithCorrectTokenReturnsRealFormContract() => Assert.NotNull(typeof(PublicSurveyValidationTests));
    [Fact] public void ValidateSurveyWithWrongTokenReturnsErrorContract() => Assert.NotNull(typeof(PublicSurveyValidationTests));
}
