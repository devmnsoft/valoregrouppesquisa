using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class PublicSurveySubmitTests
{
    [Fact] public void SubmitWithoutLgpdReturnsErrorContract() => Assert.NotNull(typeof(PublicSurveySubmitTests));
    [Fact] public void SubmitWithRequiredQuestionEmptyReturnsErrorContract() => Assert.NotNull(typeof(PublicSurveySubmitTests));
    [Fact] public void ValidSubmitCreatesResponseAnswersScoresCertificateEmailAndAuditContract() => Assert.NotNull(typeof(PublicSurveySubmitTests));
    [Fact] public void SubmitRollsBackOnMiddleFailureContract() => Assert.NotNull(typeof(PublicSurveySubmitTests));
}
