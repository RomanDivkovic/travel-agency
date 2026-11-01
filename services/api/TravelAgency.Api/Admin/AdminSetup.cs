using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Application.Admin;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Api.Admin;

public static class AdminSetup
{
    public const string Policy = "Administrator";
    public static void AddAdminFeature(this WebApplicationBuilder builder)
    {
        var minutes = builder.Configuration.GetValue("Admin:SessionMinutes", 30);
        if (minutes is < 1 or > 120) throw new InvalidOperationException("Admin session must be 1–120 minutes.");
        builder.Services.AddIdentity<Administrator, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireUppercase = true; options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true; options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.SignIn.RequireConfirmedEmail = true;
        }).AddEntityFrameworkStores<TravelAgencyDbContext>().AddDefaultTokenProviders();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = builder.Environment.IsDevelopment() ? "travel.admin" : "__Host-travel.admin";
            options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.Path = "/"; options.ExpireTimeSpan = TimeSpan.FromMinutes(minutes); options.SlidingExpiration = false;
            options.Events.OnValidatePrincipal = async context =>
            {
                await SecurityStampValidator.ValidatePrincipalAsync(context);
                context.ShouldRenew = false; // Revalidate roles/stamps without reissuing the absolute-lifetime cookie.
            };
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        // Validate account/security stamp on every protected request; logout revokes all this admin's sessions.
        builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
        builder.Services.AddAuthorization(o => o.AddPolicy(Policy, p => p.RequireAuthenticatedUser().RequireRole(Policy)));
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = builder.Environment.IsDevelopment() ? "travel.csrf" : "__Host-travel.csrf";
            options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddScoped<AdminProvisioner>();
        builder.Services.AddScoped<IAdminQueries, AdminQueries>(); builder.Services.AddScoped<IAdminMutations, AdminMutations>();
        builder.Services.AddRateLimiter(o =>
        {
            o.AddPolicy("admin-login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
            { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
        });
        // Search strings can contain customer email/name; suppress default URL/query and SQL command logs.
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    }
}
