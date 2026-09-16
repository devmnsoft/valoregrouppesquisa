using System.Text.RegularExpressions;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed partial class RazorViewCompilationRegressionTests {
    [Fact]
    public void Mvc_views_do_not_declare_page_variables_that_conflict_with_the_razor_directive() {
        var violations = MvcViews()
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
            .Where(candidate => PageVariableDeclaration().IsMatch(candidate.Line))
            .Select(candidate => $"{Path.GetRelativePath(RepositoryPaths.WebRoot, candidate.Path)}:{candidate.Number}")
            .ToArray();

        Assert.True(violations.Length == 0,
            $"Use currentPage, pageModel or pagination in MVC views:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Page_directives_are_kept_out_of_mvc_views() {
        var violations = MvcViews()
            .Where(path => File.ReadLines(path).Any(line => PageDirective().IsMatch(line)))
            .Select(path => Path.GetRelativePath(RepositoryPaths.WebRoot, path))
            .ToArray();

        Assert.True(violations.Length == 0,
            $"@page is valid in Razor Pages, not in Views:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IEnumerable<string> MvcViews() => Directory.EnumerateFiles(
        RepositoryPaths.WebFile("Views"), "*.cshtml", SearchOption.AllDirectories);

    [GeneratedRegex(@"\b(?:var|object|string|int|long)\s+page\b", RegexOptions.IgnoreCase)]
    private static partial Regex PageVariableDeclaration();

    [GeneratedRegex(@"^\s*@page(?:\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex PageDirective();
}
