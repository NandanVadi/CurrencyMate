using CurrencyMini.Data;
using CurrencyMini.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cookie login: after logging in, the browser holds an encrypted cookie that identifies the user on every request.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";          // where anonymous visitors are sent
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "CurrencyMini.Auth";
        options.Cookie.HttpOnly = true;                // not readable by JavaScript
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14); // how long "Keep me logged in" lasts
        options.SlidingExpiration = true;
    });

// Login is required everywhere by default; only actions marked [AllowAnonymous] (login/register) are public.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Typed HttpClient: ASP.NET Core creates CurrencyService and hands it a pre-configured HttpClient (DI).
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExchangeRateApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30); // history/time-series calls can be slower than single-rate calls
});

var app = builder.Build();

// Minimal setup: create the SQLite database/table on first run (no migrations needed for this scope).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        return Task.CompletedTask;
    });
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Currency}/{action=Index}/{id?}");
app.Run();
