using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Infrastructure;
using SMPP.Infrastructure.Files;
using SMPP.Infrastructure.Persistence;
using SMPP.Web;
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

builder.Services.AddControllersWithViews(options =>
{
    // Every page requires authentication by default; Account actions opt out with [AllowAnonymous].
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
    options.Filters.AddService<CheckBlacklistFilter>();
})
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o =>
        o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

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
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Off by default: the app now shares smpp_bulk_db_new with the legacy Laravel app and the
    // SMPP daemon, and EF's migration history does not describe the legacy tables it would find
    // there. Applying migrations blind would rewrite historys/under_process out from under the
    // daemon, and MySQL does not roll back DDL. Apply schema changes with a reviewed script
    // instead (see deploy/README.md), or set Database:AutoMigrate=true on a database EF owns.
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        var db = scope.ServiceProvider.GetRequiredService<SmppDbContext>();
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
