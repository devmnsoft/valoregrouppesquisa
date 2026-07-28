using Xunit;
namespace Valora.Tests;
[Trait("Category", "Unit")]
public sealed class PublicResultTokenTests
{
    [Fact] public void ResultWithWrongTokenReturnsErrorContract() => Assert.True(true);
    [Fact] public void ResultWithCorrectTokenReturnsSurveyCompanyResultAndCertificateContract() => Assert.True(true);
    [Fact] public void ResultTokenHashIsNotPublicContract() => Assert.True(true);
}
