using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Customers;
using TravelAgency.Domain.Customers;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Requests;

namespace TravelAgency.Infrastructure.Customers;

public sealed class CustomerActions(TravelAgencyDbContext db, UserManager<Administrator> users, CustomerTokenCodec codec, CustomerSettings settings, TimeProvider clock)
{
    public async Task<Administrator> LockUserAsync(string id, CancellationToken ct)
    {
        await db.Database.SqlQuery<string>($"SELECT \"Id\" AS \"Value\" FROM \"AspNetUsers\" WHERE \"Id\" = {id} FOR UPDATE").ToArrayAsync(ct);
        var u = await db.Users.SingleOrDefaultAsync(u => u.Id == id && u.IsCustomer && !u.CustomerDisabled, ct) ?? throw new CustomerOperationException(404);
        // Reload when an earlier UserManager lookup tracked this instance before the row lock.
        await db.Entry(u).ReloadAsync(ct); return u;
    }
    public void Audit(string id, string type) => db.CustomerSecurityEvents.Add(new() { CustomerId = id, Type = type, CreatedAt = clock.GetUtcNow() });
    public void SecurityMail(Administrator user, string kind = "customer-security") => db.NotificationOutbox.Add(new NotificationRecord { CustomerId = user.Id, Kind = kind, PayloadJson = JsonSerializer.Serialize(new AccountMailSnapshot(user.Email!, user.FirstName, user.PreferredLanguage)), NextAttemptAt = clock.GetUtcNow() });
    public async Task QueueAsync(Administrator u, CustomerActionPurpose purpose, Guid? requestId, CancellationToken ct)
    {
        settings.RequireDelivery(); var now = clock.GetUtcNow();
        if (await db.CustomerActions.AnyAsync(a => a.CustomerId == u.Id && a.Purpose == purpose && a.RequestId == requestId && a.CreatedAt > now.AddMinutes(-1), ct)) return;
        await db.CustomerActions.Where(a => a.CustomerId == u.Id && a.Purpose == purpose && a.RequestId == requestId && a.ConsumedAt == null && a.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(a => a.RevokedAt, now), ct);
        var raw = CustomerTokenCodec.Create();
        var identityToken = purpose switch { CustomerActionPurpose.VerifyEmail => await users.GenerateEmailConfirmationTokenAsync(u), CustomerActionPurpose.ResetPassword => await users.GeneratePasswordResetTokenAsync(u), _ => null };
        var action = new CustomerAccountAction { CustomerId = u.Id, RequestId = requestId, Purpose = purpose, Hash = CustomerTokenCodec.Hash(raw)!, SecurityStamp = u.SecurityStamp!, ProtectedIdentityToken = identityToken is null ? null : codec.ProtectIdentity(identityToken), CreatedAt = now, ExpiresAt = now.AddMinutes(settings.ActionMinutes) };
        var recipient = requestId is { } request ? await db.TravelRequests.Where(r => r.Id == request).Select(r => r.Contact.Email).SingleAsync(ct) : u.Email!;
        db.CustomerActions.Add(action); var route = purpose switch { CustomerActionPurpose.VerifyEmail => "/verify-email", CustomerActionPurpose.ResetPassword => "/reset-password", _ => "/confirm-request" };
        db.NotificationOutbox.Add(new NotificationRecord { CustomerId = u.Id, CustomerActionId = action.Id, Kind = purpose switch { CustomerActionPurpose.VerifyEmail => "customer-verify", CustomerActionPurpose.ResetPassword => "customer-reset", _ => "customer-claim" }, PayloadJson = JsonSerializer.Serialize(new AccountMailSnapshot(recipient, u.FirstName, u.PreferredLanguage)), ProtectedLink = codec.Protect(settings.PublicOrigin + route + "#" + raw), NextAttemptAt = now });
    }
    public async Task RequestAccountActionAsync(string email, CustomerActionPurpose purpose, CancellationToken ct)
    {
        settings.RequireDelivery(); if (!CustomerValidation.Email(email)) throw new CustomerOperationException(400);
        var found = await users.FindByEmailAsync(email.Trim()); if (found is null || !found.IsCustomer || found.CustomerDisabled || await users.IsInRoleAsync(found, "Administrator")) return;
        await using var tx = await db.Database.BeginTransactionAsync(ct); var user = await LockUserAsync(found.Id, ct);
        if ((purpose == CustomerActionPurpose.VerifyEmail && user.EmailConfirmed) || (purpose == CustomerActionPurpose.ResetPassword && !user.EmailConfirmed)) return;
        await QueueAsync(user, purpose, null, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ConsumeAsync(string raw, CustomerActionPurpose purpose, string? password, string? confirmation, CancellationToken ct)
    {
        var hash = CustomerTokenCodec.Hash(raw) ?? throw new CustomerOperationException(400); var userId = await db.CustomerActions.Where(a => a.Hash == hash && a.Purpose == purpose).Select(a => a.CustomerId).SingleOrDefaultAsync(ct) ?? throw new CustomerOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct); var u = await LockUserAsync(userId, ct); var a = await db.CustomerActions.SingleAsync(a => a.Hash == hash, ct);
        if (!a.Available(clock.GetUtcNow()) || a.SecurityStamp != u.SecurityStamp || await users.IsInRoleAsync(u, "Administrator")) throw new CustomerOperationException(400);
        IdentityResult result;
        if (purpose == CustomerActionPurpose.VerifyEmail) { if (u.EmailConfirmed) throw new CustomerOperationException(400); result = await users.ConfirmEmailAsync(u, codec.ReadIdentity(a.ProtectedIdentityToken!)); }
        else { if (purpose != CustomerActionPurpose.ResetPassword || !u.EmailConfirmed) throw new CustomerOperationException(400); CustomerAccounts.Password(password, confirmation); result = await users.ResetPasswordAsync(u, codec.ReadIdentity(a.ProtectedIdentityToken!), password!); }
        if (!result.Succeeded) throw new CustomerOperationException(400);
        if (purpose == CustomerActionPurpose.ResetPassword) { await users.ResetAccessFailedCountAsync(u); await users.SetLockoutEndDateAsync(u, null); }
        a.ConsumedAt = clock.GetUtcNow(); a.VersionToken = Guid.NewGuid(); u.UpdatedAt = clock.GetUtcNow(); Audit(userId, purpose == CustomerActionPurpose.VerifyEmail ? "EmailVerified" : "PasswordReset");
        if (purpose == CustomerActionPurpose.ResetPassword) SecurityMail(u); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
