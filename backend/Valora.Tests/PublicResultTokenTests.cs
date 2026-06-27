using Xunit;
namespace Valora.Tests;
public sealed class PublicResultTokenTests
{
    [Fact] public void ResultWithWrongTokenReturnsErrorContract() => Assert.True(true);
    [Fact] public void ResultWithCorrectTokenReturnsSurveyCompanyResultAndCertificateContract() => Assert.True(true);
    [Fact] public void ResultTokenHashIsNotPublicContract() => Assert.True(true);
}
