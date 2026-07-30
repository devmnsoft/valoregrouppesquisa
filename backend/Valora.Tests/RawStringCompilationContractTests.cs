using System.Text.RegularExpressions;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed partial class RawStringCompilationContractTests
{
    [Fact]
    public void InterpolatedMultilineRawStringsStartTheirContentOnTheNextLine()
    {
        var backend = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var violations = Directory
            .EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, number = index + 1 }))
            .Where(candidate => SameLineInterpolatedRawString().IsMatch(candidate.line))
            .Select(candidate => $"{Path.GetRelativePath(backend, candidate.path)}:{candidate.number}")
            .ToArray();

        Assert.True(violations.Length == 0, $"Raw strings must begin on a new line:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [GeneratedRegex("\\$\\\"\\\"\\\"\\S")]
    private static partial Regex SameLineInterpolatedRawString();
}
