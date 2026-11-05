using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TravelAgency.Api.Customers;
using TravelAgency.Application.Admin;
using TravelAgency.Application.Customers;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Customers;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private static string CustomerEmail() => "customer-" + Guid.NewGuid().ToString("N") + "@example.test";
    private async Task ClaimCustomerRequest(HttpClient c, string user, Guid requestId)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var reference = await db.TravelRequests.Where(r => r.Id == requestId).Select(r => r.ReferenceNumber).SingleAsync(Ct);
        using var sent = await CustomerMutate(c, "/requests/claim", new CustomerClaimInput(reference)); Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        using var confirmed = await CustomerMutate(c, "/requests/confirm-claim", new CustomerTokenInput(await CustomerActionToken(user, "customer-claim"))); Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerCrossAccountReadsWritesAndClaimsCannotExposeOtherCustomers()
    {
        using var admin = Client(); await Login(admin); var emailA = CustomerEmail(); var emailB = CustomerEmail(); var a = await ApprovedBooking(admin, contactEmail: emailA); var b = await ApprovedBooking(admin, contactEmail: emailB);
        using var ca = Client(); using var cb = Client(); var ua = await RegisterCustomer(ca, emailA); var ub = await RegisterCustomer(cb, emailB); await CustomerLogin(ca, emailA); await CustomerLogin(cb, emailB);
        await ClaimCustomerRequest(ca, ua.Id, a.Booking.RequestId); await ClaimCustomerRequest(cb, ub.Id, b.Booking.RequestId);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var invoice = await db.Invoices.SingleAsync(i => i.RequestId == b.Booking.RequestId, Ct); var quote = await db.Quotations.SingleAsync(q => q.Id == invoice.QuotationId, Ct);
        foreach (var path in new[] { $"travel-requests/{b.Booking.RequestId}", $"quotations/{quote.Id}", $"invoices/{invoice.Id}", $"invoices/{invoice.Id}/document", $"proposals/{b.Booking.ProposalId}", $"bookings/{b.Booking.Id}", $"activity?requestId={b.Booking.RequestId}" }) { using var r = await ca.GetAsync("/api/customer/" + path, Ct); Assert.Equal(HttpStatusCode.NotFound, r.StatusCode); Assert.DoesNotContain("SECRET", await r.Content.ReadAsStringAsync(Ct)); }
        foreach (var path in new[] { $"quotations/{quote.Id}/accept", $"invoices/{invoice.Id}/payment-session", $"proposals/{b.Booking.ProposalId}/approve" }) { using var r = await CustomerMutate(ca, "/" + path, path.Contains("proposals", StringComparison.Ordinal) ? (object)new { version = 1, confirmed = true, alternativeId = Guid.NewGuid(), termsVersion = "TEST" } : new { confirmed = true }); Assert.Equal(HttpStatusCode.NotFound, r.StatusCode); }
        var refB = await db.TravelRequests.Where(r => r.Id == b.Booking.RequestId).Select(r => r.ReferenceNumber).SingleAsync(Ct); using var noClaim = await CustomerMutate(ca, "/requests/claim", new CustomerClaimInput(refB)); Assert.Equal(HttpStatusCode.Accepted, noClaim.StatusCode);
        Assert.False(await db.CustomerActions.AnyAsync(x => x.CustomerId == ua.Id && x.RequestId == b.Booking.RequestId, Ct));
        using var own = await ca.GetAsync($"/api/customer/bookings/{a.Booking.Id}", Ct); Assert.Equal(HttpStatusCode.OK, own.StatusCode); var text = await own.Content.ReadAsStringAsync(Ct); foreach (var secret in new[] { "SECRET", "versionToken", "createdByAdministratorId", "confirmationEvidence", "paymentArrangement" }) Assert.DoesNotContain(secret, text);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerPasswordRecoveryIsNeutralSingleUseAndRevokesSessions()
    {
        using var a = Client(); var u = await RegisterCustomer(a); await CustomerLogin(a, u.Email); using var b = Client(); await CustomerLogin(b, u.Email);
        using var absent = await CustomerMutate(a, "/auth/forgot-password", new CustomerEmailInput(CustomerEmail())); using var actual = await CustomerMutate(a, "/auth/forgot-password", new CustomerEmailInput(u.Email)); Assert.Equal(HttpStatusCode.Accepted, absent.StatusCode); Assert.Equal(absent.StatusCode, actual.StatusCode); Assert.Equal(await absent.Content.ReadAsStringAsync(Ct), await actual.Content.ReadAsStringAsync(Ct));
        var raw = await CustomerActionToken(u.Id, "customer-reset"); const string nextPassword = "Changed-password!8Bb";
        using var reset = await CustomerMutate(a, "/auth/reset-password", new CustomerResetInput(raw, nextPassword, nextPassword)); Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        using var expiredSession = await b.GetAsync("/api/customer/auth/me", Ct); Assert.Equal(HttpStatusCode.Unauthorized, expiredSession.StatusCode);
        using var replay = await CustomerMutate(a, "/auth/reset-password", new CustomerResetInput(raw, nextPassword, nextPassword)); Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        using var old = await CustomerMutate(a, "/auth/login", new CustomerLogin(u.Email, CustomerPassword)); Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode); await CustomerLogin(a, u.Email, nextPassword);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerLockoutCanBeRecoveredOnlyThroughVerifiedPasswordReset()
    {
        using var c = Client(); var u = await RegisterCustomer(c);
        for (var n = 0; n < 5; n++) { using var bad = await CustomerMutate(c, "/auth/login", new CustomerLogin(u.Email, "wrong-password")); Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode); }
        using var locked = await CustomerMutate(c, "/auth/login", new CustomerLogin(u.Email, CustomerPassword)); Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        using var resetRequest = await CustomerMutate(c, "/auth/forgot-password", new CustomerEmailInput(u.Email)); Assert.Equal(HttpStatusCode.Accepted, resetRequest.StatusCode); var raw = await CustomerActionToken(u.Id, "customer-reset");
        using var reset = await CustomerMutate(c, "/auth/reset-password", new CustomerResetInput(raw, CustomerPassword, CustomerPassword)); Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode); await CustomerLogin(c, u.Email);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerVerificationResendRevokesOlderTokensAndExpiredTokensCannotVerify()
    {
        using var c = Client(); var u = await RegisterCustomer(c, verify: false); var old = await CustomerActionToken(u.Id, "customer-verify");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.CustomerActions.Where(a => a.CustomerId == u.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.CreatedAt, DateTimeOffset.UtcNow.AddMinutes(-2)), Ct); }
        using var resend = await CustomerMutate(c, "/auth/resend-verification", new CustomerEmailInput(u.Email)); Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode); var current = await CustomerActionToken(u.Id, "customer-verify"); Assert.NotEqual(old, current);
        using var revoked = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(old)); Assert.Equal(HttpStatusCode.BadRequest, revoked.StatusCode);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); await db.CustomerActions.Where(a => a.CustomerId == u.Id && a.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(a => a.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)), Ct); }
        using var expired = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(current)); Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        using var invalid = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(new string('A', 43))); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerClaimsAreBoundToCustomerExpireAndSerializeConcurrentConsumption()
    {
        using var admin = Client(); await Login(admin); var q = await Prepare(admin, contactEmail: CustomerEmail()); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var r = await db.TravelRequests.SingleAsync(r => r.Id == q.RequestId, Ct);
        using var a = Client(); var ua = await RegisterCustomer(a, r.Contact.Email); await CustomerLogin(a, ua.Email); using var b = Client(); var ub = await RegisterCustomer(b); await CustomerLogin(b, ub.Email);
        using var issue = await CustomerMutate(a, "/requests/claim", new CustomerClaimInput(r.ReferenceNumber)); Assert.Equal(HttpStatusCode.Accepted, issue.StatusCode); var raw = await CustomerActionToken(ua.Id, "customer-claim");
        using var wrong = await CustomerMutate(b, "/requests/confirm-claim", new CustomerTokenInput(raw)); Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        await db.CustomerActions.Where(x => x.CustomerId == ua.Id && x.Purpose == CustomerActionPurpose.ClaimRequest).ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)), Ct);
        using var expired = await CustomerMutate(a, "/requests/confirm-claim", new CustomerTokenInput(raw)); Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        await db.CustomerActions.Where(x => x.CustomerId == ua.Id && x.Purpose == CustomerActionPurpose.ClaimRequest).ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddHours(1)), Ct);
        var attempts = await Task.WhenAll(CustomerMutate(a, "/requests/confirm-claim", new CustomerTokenInput(raw)), CustomerMutate(a, "/requests/confirm-claim", new CustomerTokenInput(raw))); Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.NoContent); Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.BadRequest); foreach (var response in attempts) response.Dispose();
        Assert.Single(await db.CustomerRequests.Where(x => x.RequestId == r.Id).ToArrayAsync(Ct));
        using var again = await CustomerMutate(a, "/requests/claim", new CustomerClaimInput(r.ReferenceNumber)); using var unknown = await CustomerMutate(a, "/requests/claim", new CustomerClaimInput("NOT-A-REQUEST")); Assert.Equal(HttpStatusCode.Accepted, again.StatusCode); Assert.Equal(again.StatusCode, unknown.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerProfileCannotChangeEmailRolesOrOwnershipAndPasswordChangeRevokesOtherSessions()
    {
        using var a = Client(); var u = await RegisterCustomer(a); await CustomerLogin(a, u.Email); using var b = Client(); await CustomerLogin(b, u.Email);
        using var forbidden = await CustomerMutate(a, "/profile", new { firstName = "TEST", lastName = "TEST", phone = "", preferredLanguage = "sv", email = "different@example.test", roles = new[] { "Administrator" } }, HttpMethod.Patch); Assert.Equal(HttpStatusCode.BadRequest, forbidden.StatusCode);
        using var update = await CustomerMutate(a, "/profile", new CustomerProfileInput("Updated", "Customer", "+46700000000", "sv"), HttpMethod.Patch); Assert.Equal(HttpStatusCode.OK, update.StatusCode); var dto = await update.Content.ReadFromJsonAsync<CustomerUserDto>(Ct); Assert.Equal(u.Email, dto!.Email); Assert.Equal("sv", dto.PreferredLanguage);
        using var password = await CustomerMutate(a, "/auth/change-password", new CustomerPasswordInput(CustomerPassword, "Changed-password!8Bb", "Changed-password!8Bb")); Assert.Equal(HttpStatusCode.NoContent, password.StatusCode);
        using var after = await b.GetAsync("/api/customer/auth/me", Ct); Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
        using var changedLanguage = await CustomerMutate(b, "/profile/language", new CustomerLanguageInput("en"), HttpMethod.Patch); Assert.Equal(HttpStatusCode.Unauthorized, changedLanguage.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerAbsoluteSessionExpirationAndProductionCookiePolicyAreEnforced()
    {
        var clock = new CustomerTestClock(DateTimeOffset.UtcNow);
        await using var timed = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var c = Client(timed); var u = await RegisterCustomer(c); await CustomerLogin(c, u.Email); clock.Advance(TimeSpan.FromMinutes(61)); using var expired = await c.GetAsync("/api/customer/auth/me", Ct); Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        await using var production = new ApiFactory(environment: "Production", connectionString: Connection); using var scope = production.Services.CreateScope(); var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CustomerSetup.Scheme);
        Assert.Equal("__Host-travel.customer", options.Cookie.Name); Assert.True(options.Cookie.HttpOnly); Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, options.Cookie.SecurePolicy); Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Strict, options.Cookie.SameSite); Assert.False(options.SlidingExpiration);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task UnauthenticatedCustomersCannotReadOrMutatePortalResources()
    {
        using var c = Client(); var id = Guid.NewGuid();
        foreach (var path in new[] { "dashboard", "profile", "activity", "travel-requests", "quotations", "invoices", "proposals", "bookings", $"travel-requests/{id}", $"quotations/{id}", $"invoices/{id}/document", $"proposals/{id}", $"bookings/{id}" })
        { using var r = await c.GetAsync("/api/customer/" + path, Ct); Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); Assert.Contains("no-store", r.Headers.CacheControl!.ToString()); }
        using var mutation = await CustomerMutate(c, $"/quotations/{id}/accept", new { confirmed = true }); Assert.Equal(HttpStatusCode.Unauthorized, mutation.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerDuplicateRegistrationIsNeutralAndNormalizedEmailRemainsUnique()
    {
        using var c = Client(); var u = await RegisterCustomer(c);
        using var duplicate = await CustomerMutate(c, "/auth/register", new CustomerRegistration("Different", "Name", u.Email.ToUpperInvariant(), "Other-password!9Cc", "Other-password!9Cc", null, "sv")); Assert.Equal(HttpStatusCode.Accepted, duplicate.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); Assert.Equal(1, await db.Users.CountAsync(x => x.NormalizedEmail == u.Email.ToUpperInvariant(), Ct));
        await CustomerLogin(c, u.Email); var me = await c.GetFromJsonAsync<CustomerUserDto>("/api/customer/auth/me", Ct); Assert.Equal("TEST <script>", me!.FirstName);
        using var invalid = await CustomerMutate(c, "/auth/register", new CustomerRegistration("TEST", "Customer", CustomerEmail(), "weak", "weak", null, "en")); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerLoginRateLimitIsEnforcedWithRetryAfter()
    {
        using var c = Client();
        for (var i = 0; i < 10; i++) { using var r = await CustomerMutate(c, "/auth/login", new CustomerLogin("absent@example.test", CustomerPassword)); Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); }
        using var limited = await CustomerMutate(c, "/auth/login", new CustomerLogin("absent@example.test", CustomerPassword)); Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.True(limited.Headers.Contains("Retry-After"));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerNameBoundariesPreserveBothNamesWithoutExceedingLegacyIdentityField()
    {
        using var c = Client(); var email = CustomerEmail(); var first = new string('A', 100); var last = new string('B', 100);
        using var registered = await CustomerMutate(c, "/auth/register", new CustomerRegistration(first, last, email, CustomerPassword, CustomerPassword, null, "en")); Assert.Equal(HttpStatusCode.Accepted, registered.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var u = await db.Users.SingleAsync(u => u.Email == email, Ct); customerIds.Add(u.Id); Assert.Equal(first, u.FirstName); Assert.Equal(last, u.LastName); Assert.True(u.DisplayName.Length <= 100);
        using var verify = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(await CustomerActionToken(u.Id, "customer-verify"))); Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode); await CustomerLogin(c, email);
        using var changed = await CustomerMutate(c, "/profile", new CustomerProfileInput(last, first, null, "en"), HttpMethod.Patch); Assert.Equal(HttpStatusCode.OK, changed.StatusCode); var dto = await changed.Content.ReadFromJsonAsync<CustomerUserDto>(Ct); Assert.Equal(last, dto!.FirstName); Assert.Equal(first, dto.LastName);
    }
    private sealed class CustomerTestClock(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset now = initial;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan by) => now += by;
    }
}
