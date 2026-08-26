using Serilog;
using Valora.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Valora.Web.Services.Bff;
using Valora.Web.Ui;
using Valora.Web.Navigation;
using Valora.Web.Services;
using Valora.Application.Common;
using Valora.Application.DependencyInjection;
using Valora.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ValoraIconRegistry>();
builder.Services.AddSingleton<NavigationCatalog>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOrganizationProvider, CurrentOrganizationProvider>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<INavigationRouteResolver, EndpointNavigationRouteResolver>();
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = isDevelopment ? "Valora.Session" : "__Host-Valora.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/error/403";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Math.Clamp(
            builder.Configuration.GetValue("Authentication:SessionMinutes", 30), 5, 720));
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddDataProtection().SetApplicationName("Valora.Web.Bff");
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
