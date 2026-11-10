using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC with global antiforgery validation and friendly API error handling ----
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ApiExceptionFilter>();
});

// ---- Session-based authentication state (server-side session, HttpOnly cookie) ----
var idleMinutes = builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 120);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(idleMinutes);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserSession, UserSession>();

// ---- Typed HttpClient for the Part 2 API (the only data source) ----
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                 ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
if (!apiBaseUrl.EndsWith('/')) apiBaseUrl += "/";
builder.Services.AddHttpClient<RaceDayApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Api:TimeoutSeconds", 30));
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

var app = builder.Build();

app.Logger.LogInformation("RaceDay.Web using API at {ApiBaseUrl}", apiBaseUrl);

// Friendly error pages in every environment, so users never see a raw stack trace.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
if (!app.Environment.IsDevelopment()) app.UseHsts();

// One fixed culture so decimals ("21.1 km") and dates bind the same on every machine and inside Docker.
var culture = new CultureInfo("en-GB");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture },
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
