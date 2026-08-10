using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using SMPP.Application.Abstractions;
using SMPP.Infrastructure;
using SMPP.Infrastructure.Files;
using SMPP.Infrastructure.Persistence;
using SMPP.Web;
using SMPP.Web.Api;
using SMPP.Web.Filters;
using SMPP.Web.Seed;
using SMPP.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<StorageOptions>(o => o.RootPath = builder.Environment.WebRootPath);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<CheckBlacklistFilter>();

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");

// Bearer-token auth, CORS, and the OpenAPI document for the /api/v1 surface. Registered before
// the MVC services because it contributes the authentication scheme the API controllers name.
builder.Services.AddSmppApi(builder.Configuration);

builder.Services.AddControllers(options =>
{
    // Every page requires authentication by default; Account actions opt out with [AllowAnonymous].
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
    options.Filters.AddService<CheckBlacklistFilter>();
    options.Filters.Add<ApiExceptionFilter>();
})
    .AddSmppApiConventions();

var supportedUICultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "en"),
    SupportedCultures = new[] { new CultureInfo("en") },
    SupportedUICultures = supportedUICultures,
};
localizationOptions.RequestCultureProviders = new IRequestCultureProvider[]
{
    new CookieRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider(),
};

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // API endpoints accept the cookie as well as a bearer token, so an unauthenticated /api call
    // would otherwise be answered with a 302 to the HTML login page. Status codes instead - a
    // client wants to know it needs to authenticate, not receive a sign-in form.
    options.Events.OnRedirectToLogin = context => WriteApiStatusOrRedirect(context, StatusCodes.Status401Unauthorized);
    options.Events.OnRedirectToAccessDenied = context => WriteApiStatusOrRedirect(context, StatusCodes.Status403Forbidden);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (app.Configuration.GetValue("Database:UseInMemory", defaultValue: false))
    {
        // No migrations to run against the in-memory provider - build the schema straight from
        // the current EF model instead.
        await scope.ServiceProvider.GetRequiredService<SmppDbContext>().Database.EnsureCreatedAsync();
    }
    else if (app.Configuration.GetValue("Database:AutoMigrate", defaultValue: true))
    {
        // DatabaseBootstrapper, not MigrateAsync directly: against the shared smpp_bulk_db_new it
        // creates the app's own tables and stamps the baseline instead of running migrations that
        // would rewrite the SMPP daemon's historys/under_process. See its class comment.
        // Set Database:AutoMigrate=false to take the schema into your own hands.
        await scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>().BootstrapAsync();
    }

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

// CORS (so a browser app on another origin can call the API) and the Swagger page. Between
// routing and authentication, which is where a CORS policy has to sit for preflight requests
// to be answered before anything asks them to authenticate.
app.UseSmppApi();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Angular SPA: any request that doesn't match an API/Swagger endpoint above falls through to
// index.html, letting the client-side router handle deep links (e.g. a hard refresh on /customers).
app.MapFallbackToFile("index.html");

app.Run();

// An unauthenticated call under /api gets a status code and a JSON body; a browser hitting a
// page still gets the sign-in redirect.
static Task WriteApiStatusOrRedirect(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    context.Response.StatusCode = statusCode;
    return context.Response.WriteAsJsonAsync(new ApiErrorResponse(
        statusCode == StatusCodes.Status401Unauthorized
            ? "Authentication required. Send an 'Authorization: Bearer <token>' header - get a token from POST /api/v1/auth/login."
            : "Your account does not have access to this resource."));
}
