using Serilog;
using Valora.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Valora.Web.Services.Bff;
using Valora.Web.Ui;
using Valora.Web.Navigation;
using Valora.Web.Services;
using Valora.Application.Common;
using Valora.Application.DependencyInjection;
using Valora.Infrastructure.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ValoraIconRegistry>();
builder.Services.AddSingleton<PageExperienceCatalog>();
builder.Services.AddSingleton<NavigationCatalog>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOrganizationProvider, CurrentOrganizationProvider>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<INavigationRouteResolver, EndpointNavigationRouteResolver>();
var isDevelopment = builder.Environment.IsDevelopment();
var sessionMinutes = Math.Clamp(builder.Configuration.GetValue("Authentication:SessionMinutes", 30), 5, 720);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = isDevelopment ? "Valora.Session" : "__Host-Valora.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/error/403";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionMinutes);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context => WriteBffAuthenticationFailure(context, StatusCodes.Status401Unauthorized,
            "AUTHENTICATION_REQUIRED", "Sua sessão expirou. Entre novamente para continuar.");
        options.Events.OnRedirectToAccessDenied = context => WriteBffAuthenticationFailure(context, StatusCodes.Status403Forbidden,
            "ACCESS_DENIED", "Você não tem permissão para executar esta ação.");
    });
builder.Services.AddAuthorization(options =>
{
    // Internal MVC pages are private by default. Public controllers/actions must make
    // that decision explicit with [AllowAnonymous], preventing newly added screens
    // from accidentally exposing the authenticated shell or organization data.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Valora.Web.Bff");
var keyDirectory = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keyDirectory))
{
    var absoluteKeyDirectory = Path.IsPathRooted(keyDirectory)
        ? keyDirectory
        : Path.Combine(builder.Environment.ContentRootPath, keyDirectory);
    Directory.CreateDirectory(absoluteKeyDirectory);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(absoluteKeyDirectory));
}
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<BffSessionProtector>();
builder.Services.AddSingleton<IDistributedBffSessionStore, DistributedBffSessionStore>();
builder.Services.AddHostedService<BffSessionCleanupService>();
builder.Services.AddScoped<BffAuthenticationService>();
builder.Services.AddHttpClient<IBffApiClient, BffApiClient>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs);
});

builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection("Api"));

builder.Services.Configure<WebAppOptions>(
    builder.Configuration.GetSection("WebApp"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-ID";
    var incoming = context.Request.Headers[header].FirstOrDefault();
    var correlationId = !string.IsNullOrWhiteSpace(incoming) && incoming.Length <= 128
        ? incoming
        : Guid.NewGuid().ToString("N");
    context.TraceIdentifier = correlationId;
    context.Response.Headers[header] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'; object-src 'none'; base-uri 'self'";
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static Task WriteBffAuthenticationFailure(RedirectContext<CookieAuthenticationOptions> context, int status,
    string code, string message)
{
    if (!context.Request.Path.StartsWithSegments("/bff"))
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status,
        code,
        message,
        correlationId = context.HttpContext.TraceIdentifier
    }));
}
