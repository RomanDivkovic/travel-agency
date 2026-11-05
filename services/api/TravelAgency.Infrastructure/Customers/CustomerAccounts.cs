using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Customers;
using TravelAgency.Domain.Customers;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Customers;

public sealed class CustomerAccounts(TravelAgencyDbContext db, UserManager<Administrator> users, RoleManager<IdentityRole> roles, SignInManager<Administrator> signIn, CustomerActions actions, CustomerSettings settings, TimeProvider clock)
{
    private static readonly Administrator Dummy = new();
    private static readonly Lazy<string> DummyHash = new(() => new PasswordHasher<Administrator>().HashPassword(Dummy, "Not-a-customer-password!7Aa"));
    // The inherited administrator display field is shorter than the two customer name fields.
    private static string LegacyDisplayName(string first, string last) => first.Length + last.Length + 1 <= 100 ? first + " " + last : first;
    public static CustomerUserDto Dto(Administrator u) => new(u.Id, u.FirstName, u.LastName, u.Email!, u.PhoneNumber, u.PreferredLanguage, u.LastLoginAt);
    public static void ValidateProfile(CustomerProfileInput? i)
    {
        if (i is null || !CustomerValidation.Text(i.FirstName, 100) || !CustomerValidation.Text(i.LastName, 100) || !CustomerValidation.Text(i.Phone ?? "", 40, false) || !CustomerValidation.Language(i.PreferredLanguage)) throw new CustomerOperationException(400);
    }
    public static void Password(string? password, string? confirmation)
    {
        if (password is not { Length: >= 12 and <= 256 } || password != confirmation || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(c => c is >= '0' and <= '9') || !password.Any(c => !char.IsLetterOrDigit(c))) throw new CustomerOperationException(400);
    }
    public async Task RegisterAsync(CustomerRegistration input, CancellationToken ct)
    {
        settings.RequireDelivery(); ValidateProfile(new(input.FirstName, input.LastName, input.Phone, input.PreferredLanguage)); Password(input.Password, input.PasswordConfirmation);
        if (!CustomerValidation.Email(input.Email)) throw new CustomerOperationException(400);
        if (await users.FindByEmailAsync(input.Email.Trim()) is not null) { users.PasswordHasher.VerifyHashedPassword(Dummy, DummyHash.Value, input.Password); return; }
        await using var tx = await db.Database.BeginTransactionAsync(ct); var now = clock.GetUtcNow();
        var user = new Administrator { IsCustomer = true, UserName = input.Email.Trim(), Email = input.Email.Trim(), FirstName = input.FirstName.Trim(), LastName = input.LastName.Trim(), DisplayName = LegacyDisplayName(input.FirstName.Trim(), input.LastName.Trim()), PhoneNumber = input.Phone?.Trim(), PreferredLanguage = input.PreferredLanguage, CreatedAt = now, UpdatedAt = now };
        var result = await users.CreateAsync(user, input.Password); if (!result.Succeeded) { if (result.Errors.Any(e => e.Code.StartsWith("Duplicate", StringComparison.Ordinal))) return; throw new CustomerOperationException(400); }
        // Serialize initial role creation without changing existing administrator roles.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(709104)", ct);
        if (!await roles.RoleExistsAsync("Customer") && !(await roles.CreateAsync(new IdentityRole("Customer"))).Succeeded) throw new CustomerOperationException(503);
        if (!(await users.AddToRoleAsync(user, "Customer")).Succeeded) throw new CustomerOperationException(503);
        actions.Audit(user.Id, "Registered"); await actions.QueueAsync(user, CustomerActionPurpose.VerifyEmail, null, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<Administrator?> LoginAsync(CustomerLogin input, CancellationToken ct)
    {
        if (!CustomerValidation.Email(input.Email) || input.Password is not { Length: > 0 and <= 256 }) return null;
        var found = await users.FindByEmailAsync(input.Email.Trim());
        if (found is null || !await CustomerUserAsync(found)) { users.PasswordHasher.VerifyHashedPassword(Dummy, DummyHash.Value, input.Password); return null; }
        await using var tx = await db.Database.BeginTransactionAsync(ct); var user = await actions.LockUserAsync(found.Id, ct);
        var result = await signIn.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true);
        if (!result.Succeeded) { actions.Audit(user.Id, "LoginRejected"); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return null; }
        user.LastLoginAt = clock.GetUtcNow(); actions.Audit(user.Id, "LoggedIn"); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return user;
    }
    public async Task<CustomerUserDto> ProfileAsync(string id, CustomerProfileInput input, CancellationToken ct)
    {
        ValidateProfile(input); await using var tx = await db.Database.BeginTransactionAsync(ct); var u = await actions.LockUserAsync(id, ct);
        u.FirstName = input.FirstName.Trim(); u.LastName = input.LastName.Trim(); u.DisplayName = LegacyDisplayName(u.FirstName, u.LastName); u.PhoneNumber = input.Phone?.Trim(); u.PreferredLanguage = input.PreferredLanguage; u.UpdatedAt = clock.GetUtcNow();
        if (!(await users.UpdateAsync(u)).Succeeded) throw new CustomerOperationException(409); actions.Audit(id, "ProfileUpdated"); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Dto(u);
    }
    public async Task<CustomerUserDto> LanguageAsync(string id, string language, CancellationToken ct)
    {
        if (!CustomerValidation.Language(language)) throw new CustomerOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var u = await actions.LockUserAsync(id, ct); u.PreferredLanguage = language; u.UpdatedAt = clock.GetUtcNow();
        if (!(await users.UpdateAsync(u)).Succeeded) throw new CustomerOperationException(409); await tx.CommitAsync(ct); return Dto(u);
    }
    public async Task ChangePasswordAsync(string id, CustomerPasswordInput input, CancellationToken ct)
    {
        Password(input.Password, input.PasswordConfirmation); if (input.CurrentPassword is not { Length: > 0 and <= 256 }) throw new CustomerOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var u = await actions.LockUserAsync(id, ct);
        if (!(await users.ChangePasswordAsync(u, input.CurrentPassword, input.Password)).Succeeded) throw new CustomerOperationException(400);
        actions.Audit(id, "PasswordChanged"); actions.SecurityMail(u); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RevokeSessionsAsync(string id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var u = await actions.LockUserAsync(id, ct);
        if (!(await users.UpdateSecurityStampAsync(u)).Succeeded) throw new CustomerOperationException(503);
        actions.Audit(id, "SessionsRevoked"); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<bool> CustomerUserAsync(Administrator user) => user.IsCustomer && !user.CustomerDisabled && await users.IsInRoleAsync(user, "Customer") && !await users.IsInRoleAsync(user, "Administrator");
}
