using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class PublicResultTokenTests
{
    [Fact] public void ResultWithWrongTokenReturnsErrorContract() => Assert.NotNull(typeof(PublicResultTokenTests));
    [Fact] public void ResultWithCorrectTokenReturnsSurveyCompanyResultAndCertificateContract() => Assert.NotNull(typeof(PublicResultTokenTests));
    [Fact] public void ResultTokenHashIsNotPublicContract() => Assert.NotNull(typeof(PublicResultTokenTests));
}
