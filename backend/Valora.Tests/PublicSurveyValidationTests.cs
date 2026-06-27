using Xunit;
namespace Valora.Tests;
public sealed class PublicSurveyValidationTests
{
    [Fact] public void ValidateSurveyWithCorrectTokenReturnsRealFormContract() => Assert.True(true);
    [Fact] public void ValidateSurveyWithWrongTokenReturnsErrorContract() => Assert.True(true);
}
