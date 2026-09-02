using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.Tests;

public sealed class GenerateReportRequestValidationTests
{
    [Theory]
    [InlineData("html")]
    [InlineData("csv")]
    public void Supported_formats_are_valid(string format)
    {
        Assert.Empty(Validate(new GenerateReportRequest(format)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("pdf")]
    [InlineData("../html")]
    public void Unsupported_formats_are_rejected(string format)
    {
        var errors = Validate(new GenerateReportRequest(format));

        Assert.NotEmpty(errors);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(GenerateReportRequest.Format)));
    }

    private static IReadOnlyList<ValidationResult> Validate(GenerateReportRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
