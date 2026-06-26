using Dapper;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class HealthController(
    IDbConnectionFactory factory,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get()
    {
        return Ok(new
        {
            ok = true,
            service = "Valora.Api",
            environment = environment.EnvironmentName,
            time = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("/health/database")]
    public async Task<IActionResult> Database()
    {
        using var connection = factory.Create();
        var isHealthy = await connection.ExecuteScalarAsync<int>("SELECT 1;") == 1;

        return Ok(new
        {
            ok = isHealthy,
            service = "Valora.Api",
            database = "ok",
            environment = environment.EnvironmentName,
            time = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("/health/config")]
    public IActionResult Config()
    {
        return Ok(new
        {
            ok = true,
            service = "Valora.Api",
            environment = environment.EnvironmentName,
            postgresConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")),
            time = DateTimeOffset.UtcNow
        });
    }
}
