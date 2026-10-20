using Npgsql;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Valora.Tests.Support;

internal static class PostgresTestEnvironment {
    public static bool IsRequired =>
        string.Equals(Environment.GetEnvironmentVariable("VALORA_REQUIRE_POSTGRES_TESTS"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("VALORA_REQUIRE_POSTGRES_TESTS"), "1", StringComparison.Ordinal);

    public static string MissingConnectionMessage(string reason) =>
        $"VALORA_TEST_POSTGRES_CONNECTION não configurada; {reason}";

    public static string ConnectionString(string reason) {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(MissingConnectionMessage(reason));

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Matches("(?i)(test|teste|homolog|qa)", builder.Database ?? "");
        return connectionString;
    }
}

public sealed class PostgresFactAttribute : FactAttribute {
    public PostgresFactAttribute(string reason) {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION"))
            && !PostgresTestEnvironment.IsRequired) {
            Skip = PostgresTestEnvironment.MissingConnectionMessage(reason);
        }
    }
}
