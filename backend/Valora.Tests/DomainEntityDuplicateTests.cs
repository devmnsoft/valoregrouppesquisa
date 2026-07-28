using System.Text.RegularExpressions;
using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class DomainEntityDuplicateTests
{
    [Fact]
    public void Domain_entities_do_not_repeat_type_names_in_the_same_namespace()
    {
        var root = RepositoryPaths.DomainFile("Entities");
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase));

        var declarations = files.SelectMany(file =>
        {
            var text = File.ReadAllText(file);
            var namespaceName = Regex.Match(text, @"namespace\s+([A-Za-z0-9_.]+)").Groups[1].Value;
            return Regex.Matches(text, @"\b(class|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)")
                .Select(match => new
                {
                    Namespace = string.IsNullOrWhiteSpace(namespaceName) ? "<global>" : namespaceName,
                    Kind = match.Groups[1].Value,
                    Name = match.Groups[2].Value,
                    File = Path.GetRelativePath(root, file)
                });
        }).ToArray();

        var duplicates = declarations
            .GroupBy(item => new { item.Namespace, item.Name })
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.Namespace}.{group.Key.Name}: {string.Join(", ", group.Select(item => item.File))}")
            .ToArray();

        Assert.True(duplicates.Length == 0, "Entidades duplicadas encontradas: " + string.Join("; ", duplicates));
    }
}
