using Dapper;
using Microsoft.AspNetCore.Mvc;
using Valora.Infrastructure.Database;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class HealthController(IDbConnectionFactory factory, IWebHostEnvironment env, IConfiguration config):ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get()=>Ok(new{ok=true,service="Valora.Api",environment=env.EnvironmentName,time=DateTimeOffset.UtcNow});

    [HttpGet("/health/database")]
    public async Task<IActionResult> Db(){ using var c=factory.Create(); var ok=await c.ExecuteScalarAsync<int>("SELECT 1;"); return Ok(new{ok=ok==1,service="Valora.Api",database="ok",environment=env.EnvironmentName,time=DateTimeOffset.UtcNow}); }

    [HttpGet("/health/config")]
    public IActionResult Config()=>Ok(new{ok=true,service="Valora.Api",environment=env.EnvironmentName,postgresConfigured=!string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")),time=DateTimeOffset.UtcNow});
}
