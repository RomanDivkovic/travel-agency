using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using TravelAgency.Application.Customers;
using TravelAgency.Domain.Customers;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Customers;

namespace TravelAgency.Api.Customers;

public static class CustomerAuthEndpoints
{
    public static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static void MapCustomerAuth(this WebApplication app)
    {
        var a = app.MapGroup("/api/customer/auth").AddEndpointFilter<CustomerCsrfFilter>().AddEndpointFilter<CustomerErrorFilter>();
        a.MapGet("/csrf", (HttpContext c, IAntiforgery csrf) => Results.Ok(new { token = csrf.GetAndStoreTokens(c).RequestToken }));
        a.MapPost("/register", async (CustomerRegistration input, CustomerAccounts accounts, CancellationToken ct) => { await accounts.RegisterAsync(input, ct); return Results.Accepted(); }).RequireRateLimiting("customer-account-actions");
        a.MapPost("/login", async (CustomerLogin input, CustomerAccounts accounts, SignInManager<Administrator> signIn, CustomerSettings settings, TimeProvider clock, HttpContext c, CancellationToken ct) =>
        {
            var user = await accounts.LoginAsync(input, ct); if (user is null) return Results.Problem(statusCode: 401, title: "Invalid email or password, or account not ready.");
            var principal = await signIn.CreateUserPrincipalAsync(user); ((ClaimsIdentity)principal.Identity!).AddClaim(new("travel.customer", "true")); var now = clock.GetUtcNow(); var expires = now.AddMinutes(settings.SessionMinutes);
            await c.SignInAsync(CustomerSetup.Scheme, principal, new AuthenticationProperties { IsPersistent = false, AllowRefresh = false, IssuedUtc = now, ExpiresUtc = expires });
            return Results.Ok(CustomerAccounts.Dto(user) with { SessionExpiresAt = expires });
        }).RequireRateLimiting("customer-login");
        a.MapGet("/me", async (HttpContext c, UserManager<Administrator> users) => { var u = (await users.GetUserAsync(c.User))!; var auth = await c.AuthenticateAsync(CustomerSetup.Scheme); return Results.Ok(CustomerAccounts.Dto(u) with { SessionExpiresAt = auth.Properties?.ExpiresUtc }); }).RequireAuthorization(CustomerSetup.Policy);
        a.MapPost("/logout", async (HttpContext c, CustomerAccounts accounts, CancellationToken ct) => { await accounts.RevokeSessionsAsync(Actor(c), ct); await c.SignOutAsync(CustomerSetup.Scheme); return Results.NoContent(); }).RequireAuthorization(CustomerSetup.Policy);
        a.MapPost("/change-password", async (CustomerPasswordInput input, HttpContext c, CustomerAccounts accounts, CancellationToken ct) => { await accounts.ChangePasswordAsync(Actor(c), input, ct); await c.SignOutAsync(CustomerSetup.Scheme); return Results.NoContent(); }).RequireAuthorization(CustomerSetup.Policy).RequireRateLimiting("customer-account-actions");
        a.MapPost("/verify-email", async (CustomerTokenInput input, CustomerActions actions, CancellationToken ct) => { await actions.ConsumeAsync(input.Token, CustomerActionPurpose.VerifyEmail, null, null, ct); return Results.NoContent(); }).RequireRateLimiting("customer-account-actions");
        a.MapPost("/reset-password", async (CustomerResetInput input, CustomerActions actions, HttpContext c, CancellationToken ct) => { await actions.ConsumeAsync(input.Token, CustomerActionPurpose.ResetPassword, input.Password, input.PasswordConfirmation, ct); await c.SignOutAsync(CustomerSetup.Scheme); return Results.NoContent(); }).RequireRateLimiting("customer-account-actions");
        a.MapPost("/resend-verification", async (CustomerEmailInput input, CustomerActions actions, CancellationToken ct) => { await actions.RequestAccountActionAsync(input.Email, CustomerActionPurpose.VerifyEmail, ct); return Results.Accepted(); }).RequireRateLimiting("customer-account-actions");
        a.MapPost("/forgot-password", async (CustomerEmailInput input, CustomerActions actions, CancellationToken ct) => { await actions.RequestAccountActionAsync(input.Email, CustomerActionPurpose.ResetPassword, ct); return Results.Accepted(); }).RequireRateLimiting("customer-account-actions");
        var p = app.MapGroup("/api/customer").RequireAuthorization(CustomerSetup.Policy).AddEndpointFilter<CustomerCsrfFilter>().AddEndpointFilter<CustomerErrorFilter>();
        p.MapGet("/profile", async (HttpContext c, UserManager<Administrator> users) => Results.Ok(CustomerAccounts.Dto((await users.GetUserAsync(c.User))!)));
        p.MapPatch("/profile", async (CustomerProfileInput input, HttpContext c, CustomerAccounts accounts, CancellationToken ct) => Results.Ok(await accounts.ProfileAsync(Actor(c), input, ct)));
        p.MapPatch("/profile/language", async (CustomerLanguageInput input, HttpContext c, CustomerAccounts accounts, CancellationToken ct) => Results.Ok(await accounts.LanguageAsync(Actor(c), input.PreferredLanguage, ct)));
        p.MapPost("/requests/claim", async (CustomerClaimInput input, HttpContext c, CustomerClaims claims, CancellationToken ct) => { await claims.RequestAsync(Actor(c), input.Reference, ct); return Results.Accepted(); }).RequireRateLimiting("customer-account-actions");
        p.MapPost("/requests/confirm-claim", async (CustomerTokenInput input, HttpContext c, CustomerClaims claims, CancellationToken ct) => { await claims.ConfirmAsync(Actor(c), input.Token, ct); return Results.NoContent(); }).RequireRateLimiting("customer-account-actions");
    }
}
