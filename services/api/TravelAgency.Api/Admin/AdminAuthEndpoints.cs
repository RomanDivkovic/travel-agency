using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using TravelAgency.Application.Admin;
using TravelAgency.Infrastructure.Admin;

namespace TravelAgency.Api.Admin;

public static class AdminAuthEndpoints
{
    public sealed record LoginInput(string? Email, string? Password);
    private static AdminUserDto Dto(Administrator user) => new(user.Id, user.DisplayName, user.Email!, "Administrator");
    public static void MapAdminAuth(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/auth").AddEndpointFilter<AdminCsrfFilter>();
        group.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) => Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
        group.MapPost("/login", async (LoginInput input, UserManager<Administrator> users, SignInManager<Administrator> signIn, ILoggerFactory logs, HttpContext context) =>
        {
            if (input.Email is not { Length: > 0 and <= 254 } || input.Password is not { Length: > 0 and <= 256 }) return Results.Problem(statusCode: 401, title: "Invalid email or password.");
            var user = await users.FindByEmailAsync(input.Email.Trim());
            var valid = user is not null && await users.IsInRoleAsync(user, AdminSetup.Policy);
            var result = valid ? await signIn.PasswordSignInAsync(user!, input.Password, isPersistent: false, lockoutOnFailure: true) : Microsoft.AspNetCore.Identity.SignInResult.Failed;
            if (!result.Succeeded)
            {
                logs.CreateLogger("AdminSecurity").LogWarning("Administrator login rejected. Trace {TraceId}", context.TraceIdentifier);
                return Results.Problem(statusCode: 401, title: "Invalid email or password.");
            }
            logs.CreateLogger("AdminSecurity").LogInformation("Administrator {ActorId} logged in. Trace {TraceId}", user!.Id, context.TraceIdentifier);
            return Results.Ok(Dto(user));
        }).RequireRateLimiting("admin-login");
        group.MapGet("/me", async (HttpContext context, UserManager<Administrator> users) => Results.Ok(Dto((await users.GetUserAsync(context.User))!))).RequireAuthorization(AdminSetup.Policy);
        group.MapPost("/logout", async (HttpContext context, UserManager<Administrator> users, SignInManager<Administrator> signIn, ILoggerFactory logs) =>
        {
            var user = (await users.GetUserAsync(context.User))!;
            var result = await users.UpdateSecurityStampAsync(user);
            if (!result.Succeeded) return Results.Problem(statusCode: 503, title: "Logout could not be confirmed.");
            await signIn.SignOutAsync();
            logs.CreateLogger("AdminSecurity").LogInformation("Administrator {ActorId} logged out; sessions revoked. Trace {TraceId}", user.Id, context.TraceIdentifier);
            return Results.NoContent();
        }).RequireAuthorization(AdminSetup.Policy);
    }
}
