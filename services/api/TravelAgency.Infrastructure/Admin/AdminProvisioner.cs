using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Admin;

public sealed class AdminProvisioner(TravelAgencyDbContext db, UserManager<Administrator> users, RoleManager<IdentityRole> roles)
{
    public async Task<bool> ProvisionAsync(string email, string? password, string displayName, CancellationToken ct)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email || email.Length > 254 || string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
            throw new InvalidOperationException("Valid administrator email and display name required.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7300510001)", ct);
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, "Administrator")) throw new InvalidOperationException("Existing account is not an administrator; no changes made.");
            return false;
        }
        if (await db.Users.AnyAsync(u => !u.IsCustomer, ct) || await db.UserRoles.AnyAsync(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ADMINISTRATOR"), ct)) throw new InvalidOperationException("An account already exists. Provisioning does not create additional administrators or overwrite credentials.");
        if (string.IsNullOrEmpty(password) || password.Length > 256) throw new InvalidOperationException("Supply an initial administrator password through secure configuration.");
        if (!await roles.RoleExistsAsync("Administrator")) Ensure(await roles.CreateAsync(new IdentityRole("Administrator")));
        var admin = new Administrator { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName };
        Ensure(await users.CreateAsync(admin, password)); Ensure(await users.AddToRoleAsync(admin, "Administrator"));
        await transaction.CommitAsync(ct); return true;
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Provisioning failed. Check password policy and account configuration. No credentials were changed.");
    }
}
