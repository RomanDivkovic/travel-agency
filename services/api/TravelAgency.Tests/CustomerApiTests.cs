using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Customers;
using TravelAgency.Domain.Customers;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private readonly List<string> customerIds = [];
    private const string CustomerPassword = "TEST-customer-password!7Aa";
    private static async Task<HttpResponseMessage> CustomerMutate(HttpClient c, string path, object? data = null, HttpMethod? method = null)
    {
        var csrf = await c.GetFromJsonAsync<JsonElement>("/api/customer/auth/csrf", Ct); using var r = new HttpRequestMessage(method ?? HttpMethod.Post, "/api/customer" + path) { Content = data is null ? null : JsonContent.Create(data) }; r.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString()); return await c.SendAsync(r, Ct);
    }
    private async Task<(string Id, string Email)> RegisterCustomer(HttpClient c, string? email = null, bool verify = true)
    {
        email ??= "customer-" + Guid.NewGuid().ToString("N") + "@example.test";
        using var r = await CustomerMutate(c, "/auth/register", new CustomerRegistration("TEST <script>", "Customer", email, CustomerPassword, CustomerPassword, null, "en")); Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var u = await db.Users.SingleAsync(u => u.Email == email, Ct); customerIds.Add(u.Id);
        if (verify) { var token = await CustomerActionToken(u.Id, "customer-verify"); using var v = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(token)); Assert.Equal(HttpStatusCode.NoContent, v.StatusCode); }
        return (u.Id, email);
    }
    private async Task<string> CustomerActionToken(string id, string kind)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var n = await db.NotificationOutbox.Where(n => n.CustomerId == id && n.Kind == kind).OrderByDescending(n => n.NextAttemptAt).FirstAsync(Ct); var link = scope.ServiceProvider.GetRequiredService<CustomerTokenCodec>().Read(n.ProtectedLink!); var token = new Uri(link).Fragment[1..];
        Assert.DoesNotContain(token, n.ProtectedLink!); Assert.Equal(CustomerTokenCodec.Hash(token), (await db.CustomerActions.SingleAsync(a => a.Id == n.CustomerActionId, Ct)).Hash); return token;
    }
    private static async Task CustomerLogin(HttpClient c, string email, string? password = null)
    {
        using var r = await CustomerMutate(c, "/auth/login", new CustomerLogin(email, password ?? CustomerPassword)); Assert.Equal(HttpStatusCode.OK, r.StatusCode); Assert.Contains(r.Headers.GetValues("Set-Cookie"), s => s.Contains("travel.customer") && s.Contains("httponly", StringComparison.OrdinalIgnoreCase) && s.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CustomerRegistrationRequiresVerificationAndDoesNotGrantAdministratorAccess()
    {
        using var c = Client(); var u = await RegisterCustomer(c, verify: false);
        using var before = await CustomerMutate(c, "/auth/login", new CustomerLogin(u.Email, CustomerPassword)); Assert.Equal(HttpStatusCode.Unauthorized, before.StatusCode);
        var token = await CustomerActionToken(u.Id, "customer-verify"); using var verified = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(token)); Assert.Equal(HttpStatusCode.NoContent, verified.StatusCode);
        using var replay = await CustomerMutate(c, "/auth/verify-email", new CustomerTokenInput(token)); Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        await CustomerLogin(c, u.Email); using var admin = await c.GetAsync("/api/admin/travel-requests", Ct); Assert.Equal(HttpStatusCode.Unauthorized, admin.StatusCode);
        var me = await c.GetFromJsonAsync<CustomerUserDto>("/api/customer/auth/me", Ct); Assert.Equal(u.Id, me!.Id); Assert.NotNull(me.SessionExpiresAt);
        using var csrf = await c.PostAsJsonAsync("/api/customer/auth/logout", new { }, Ct); Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        using var logout = await CustomerMutate(c, "/auth/logout"); Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode); using var after = await c.GetAsync("/api/customer/dashboard", Ct); Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task VerifiedCustomerMustExplicitlyClaimMatchingGuestRequestBeforeAnyPortalAccess()
    {
        using var admin = Client(); await Login(admin); var quote = await Prepare(admin); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var request = await db.TravelRequests.SingleAsync(r => r.Id == quote.RequestId, Ct);
        using var customer = Client(); var u = await RegisterCustomer(customer, request.Contact.Email); await CustomerLogin(customer, u.Email);
        using var denied = await customer.GetAsync($"/api/customer/travel-requests/{request.Id}", Ct); Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        using var claim = await CustomerMutate(customer, "/requests/claim", new CustomerClaimInput(request.ReferenceNumber)); Assert.Equal(HttpStatusCode.Accepted, claim.StatusCode);
        var raw = await CustomerActionToken(u.Id, "customer-claim"); using var confirmed = await CustomerMutate(customer, "/requests/confirm-claim", new CustomerTokenInput(raw)); Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        using var details = await customer.GetAsync($"/api/customer/travel-requests/{request.Id}", Ct); Assert.Equal(HttpStatusCode.OK, details.StatusCode); var text = await details.Content.ReadAsStringAsync(Ct); Assert.DoesNotContain("versionToken", text); Assert.DoesNotContain("actorId", text);
        using var dashboard = await customer.GetAsync("/api/customer/dashboard", Ct); Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        foreach (var kind in new[] { "travel-requests", "quotations", "invoices", "proposals", "bookings", "activity" }) { using var list = await customer.GetAsync("/api/customer/" + kind, Ct); Assert.Equal(HttpStatusCode.OK, list.StatusCode); }
        using var used = await CustomerMutate(customer, "/requests/confirm-claim", new CustomerTokenInput(raw)); Assert.Equal(HttpStatusCode.BadRequest, used.StatusCode);
    }
}
