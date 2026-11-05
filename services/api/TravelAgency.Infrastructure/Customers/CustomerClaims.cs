using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Domain.Admin;
using TravelAgency.Domain.Customers;
using TravelAgency.Infrastructure.Admin;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Infrastructure.Customers;

public sealed class CustomerClaims(TravelAgencyDbContext db, CustomerActions actions, ResearchTransaction work, UserManager<Administrator> users, TimeProvider clock)
{
    public async Task RequestAsync(string customerId, string reference, CancellationToken ct)
    {
        if (!CustomerValidation.Text(reference, 40)) throw new CustomerOperationException(400);
        var id = await db.TravelRequests.Where(r => r.ReferenceNumber == reference.Trim()).Select(r => (Guid?)r.Id).SingleOrDefaultAsync(ct); if (id is null) return;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        TravelAgency.Domain.Requests.TravelInquiry request;
        try { request = await work.LockAsync(id.Value, ct); } catch (ResearchOperationException e) when (e.Status == 404) { return; }
        var user = await actions.LockUserAsync(customerId, ct);
        if (!user.EmailConfirmed || users.NormalizeEmail(request.Contact.Email) != user.NormalizedEmail || await db.CustomerRequests.AnyAsync(o => o.RequestId == id, ct)) return;
        await actions.QueueAsync(user, CustomerActionPurpose.ClaimRequest, id, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ConfirmAsync(string customerId, string raw, CancellationToken ct)
    {
        var hash = CustomerTokenCodec.Hash(raw) ?? throw new CustomerOperationException(400);
        var requestId = await db.CustomerActions.Where(a => a.Hash == hash && a.Purpose == CustomerActionPurpose.ClaimRequest && a.CustomerId == customerId).Select(a => a.RequestId).SingleOrDefaultAsync(ct) ?? throw new CustomerOperationException(400);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        TravelAgency.Domain.Requests.TravelInquiry request;
        try { request = await work.LockAsync(requestId, ct); } catch (ResearchOperationException e) when (e.Status == 404) { throw new CustomerOperationException(400); }
        var user = await actions.LockUserAsync(customerId, ct); var a = await db.CustomerActions.SingleAsync(a => a.Hash == hash, ct);
        if (!user.EmailConfirmed || !a.Available(clock.GetUtcNow()) || a.SecurityStamp != user.SecurityStamp || users.NormalizeEmail(request.Contact.Email) != user.NormalizedEmail || await db.CustomerRequests.AnyAsync(o => o.RequestId == requestId, ct)) throw new CustomerOperationException(400);
        db.CustomerRequests.Add(new() { CustomerId = customerId, RequestId = requestId, LinkedAt = clock.GetUtcNow() }); a.ConsumedAt = clock.GetUtcNow(); a.VersionToken = Guid.NewGuid();
        actions.Audit(customerId, "RequestClaimed"); actions.SecurityMail(user, "customer-linked"); db.RequestActivities.Add(new RequestActivity { RequestId = requestId, Type = "CustomerLinked", CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
public sealed class CustomerOwnership(TravelAgencyDbContext db)
{
    public IQueryable<CustomerRequestOwnership> Owned(string id) => db.CustomerRequests.Where(o => o.CustomerId == id && db.Users.Any(u => u.Id == id && u.IsCustomer && u.EmailConfirmed && !u.CustomerDisabled));
    public async Task RequireAsync(string id, Guid request, CancellationToken ct) { if (!await Owned(id).AnyAsync(o => o.RequestId == request, ct)) throw new CustomerOperationException(404); }
}
