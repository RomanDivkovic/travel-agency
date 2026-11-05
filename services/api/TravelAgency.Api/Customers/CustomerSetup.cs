using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Api.Admin;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Customers;

namespace TravelAgency.Api.Customers;

public static class CustomerSetup
{
    public const string Scheme = "TravelCustomer";
    public const string Policy = "VerifiedCustomer";
    public static void AddCustomers(this WebApplicationBuilder b)
    {
        var s = b.Configuration.GetSection("CustomerPortal").Get<CustomerSettings>() ?? new();
        if (s.SessionMinutes is < 1 or > 120 || s.ActionMinutes is < 5 or > 120) throw new InvalidOperationException("Customer session/action lifetime is invalid.");
        if (s.PublicOrigin.Length == 0 && b.Environment.IsDevelopment()) s.PublicOrigin = "http://localhost:5173";
        if (s.PublicOrigin.Length > 0)
        {
            if (!Uri.TryCreate(s.PublicOrigin, UriKind.Absolute, out var origin) || origin.UserInfo.Length > 0 || origin.AbsolutePath != "/" || origin.Query.Length > 0 || origin.Fragment.Length > 0 || (origin.Scheme != "https" && !(b.Environment.IsDevelopment() && origin.Scheme == "http" && origin.IsLoopback))) throw new InvalidOperationException("Customer origin requires HTTPS (loopback HTTP in Development only).");
            s.PublicOrigin = origin.GetLeftPart(UriPartial.Authority);
        }
        b.Services.AddSingleton(s); b.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(2));
        b.Services.AddScoped<CustomerTokenCodec>(); b.Services.AddScoped<CustomerActions>(); b.Services.AddScoped<CustomerAccounts>(); b.Services.AddScoped<CustomerClaims>(); b.Services.AddScoped<CustomerOwnership>(); b.Services.AddScoped<CustomerPortalQueries>();
        b.Services.AddAuthentication().AddCookie(Scheme, o =>
        {
            o.Cookie.Name = b.Environment.IsDevelopment() ? "travel.customer" : "__Host-travel.customer"; o.Cookie.Path = "/"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = b.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; o.ExpireTimeSpan = TimeSpan.FromMinutes(s.SessionMinutes); o.SlidingExpiration = false;
            o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; }; o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
            o.Events.OnValidatePrincipal = async c =>
            {
                var users = c.HttpContext.RequestServices.GetRequiredService<UserManager<Administrator>>(); var accounts = c.HttpContext.RequestServices.GetRequiredService<CustomerAccounts>(); var u = await users.GetUserAsync(c.Principal!);
                if (u is null || !u.EmailConfirmed || !await accounts.CustomerUserAsync(u) || c.Principal!.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType) != u.SecurityStamp) c.RejectPrincipal();
                c.ShouldRenew = false;
            };
        });
        b.Services.AddOptions<CookieAuthenticationOptions>(Scheme).Configure<TimeProvider>((o, clock) => o.TimeProvider = clock);
        b.Services.AddAuthorization(o => o.AddPolicy(Policy, p => p.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser().RequireRole("Customer").RequireClaim("travel.customer", "true")));
        b.Services.AddRateLimiter(o =>
        {
            o.AddPolicy("customer-login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("customer-account-actions", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }
}
public sealed class CustomerCsrfFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        var auth = await c.HttpContext.AuthenticateAsync(CustomerSetup.Scheme);
        c.HttpContext.User = auth.Succeeded ? auth.Principal! : new ClaimsPrincipal(new ClaimsIdentity());
        return await new AdminCsrfFilter().InvokeAsync(c, next);
    }
}
public sealed class CustomerErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        try { return await next(c); }
        catch (CustomerOperationException e) { return Results.Problem(statusCode: e.Status, title: e.Status switch { 404 => "Record unavailable.", 409 => "State changed. Reload before retrying.", 412 => "Customer account email requires an approved origin configuration.", _ => "Check account details, credentials or the expired/used link." }); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "State changed. Reload before retrying."); }
        catch (DbUpdateException e) when (c.HttpContext.Request.Path == "/api/customer/auth/register" && e.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return Results.Accepted(); }
        catch (System.Security.Cryptography.CryptographicException) { return Results.Problem(statusCode: 400, title: "Link unavailable or expired."); }
    }
}
