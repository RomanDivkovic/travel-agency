using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Api.Admin;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Api.Payments;

public static class InvoiceEndpoints
{
    public sealed record VersionInput(Guid VersionToken);
    public sealed record DraftInput(Guid VersionToken, string CustomerAddress, string ServicePeriod, DateTimeOffset DueAt);
    public static void MapInvoices(this WebApplication app)
    {
        var a = app.MapGroup("/api/admin").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>().AddEndpointFilter<PaymentErrorFilter>();
        a.MapGet("/invoices/configuration", (PaymentSettings s, QuotationSettings tax) => Results.Ok(new { s.Provider, isSimulated = s.Provider == "Sandbox", blockingReasons = s.BlockingReasons(tax) }));
        a.MapPost("/quotations/{id:guid}/invoice", async (Guid id, HttpContext c, InvoiceOperations ops, CancellationToken ct) => Results.Json(new { id = await ops.CreateAsync(id, Actor(c), ct) }, statusCode: 201));
        a.MapGet("/quotations/{id:guid}/invoice", async (Guid id, TravelAgencyDbContext db, CancellationToken ct) => Results.Ok(new { id = await db.Invoices.Where(i => i.QuotationId == id).Select(i => (Guid?)i.Id).SingleOrDefaultAsync(ct) }));
        a.MapGet("/invoices", async (int? page, string? status, string? search, InvoiceQueries q, CancellationToken ct) => Results.Ok(await q.ListAsync(page ?? 1, status, search, ct)));
        a.MapGet("/invoices/{id:guid}", async (Guid id, InvoiceQueries q, CancellationToken ct) => await q.DetailAsync(id, ct) is { } i ? Results.Ok(i) : Results.NotFound());
        a.MapPatch("/invoices/{id:guid}", async (Guid id, DraftInput input, HttpContext c, InvoiceOperations ops, CancellationToken ct) => { await ops.EditAsync(id, input.VersionToken, new(input.CustomerAddress, input.ServicePeriod, input.DueAt), Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/invoices/{id:guid}/issue", async (Guid id, VersionInput input, HttpContext c, InvoiceOperations ops, CancellationToken ct) => { await ops.IssueAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/invoices/{id:guid}/cancel", async (Guid id, VersionInput input, HttpContext c, InvoiceOperations ops, CancellationToken ct) => { await ops.CancelAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/invoices/{id:guid}/revoke-access", async (Guid id, VersionInput input, HttpContext c, InvoiceOperations ops, CancellationToken ct) => { await ops.RevokeAccessAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        a.MapGet("/invoices/{id:guid}/payments", async (Guid id, InvoiceQueries q, CancellationToken ct) => await q.DetailAsync(id, ct) is { } i ? Results.Ok(i.Payments) : Results.NotFound());
        a.MapGet("/invoices/{id:guid}/document", async (Guid id, TravelAgencyDbContext db, CancellationToken ct) => await db.Invoices.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id && i.IssuedAt != null, ct) is { } i ? Document(i) : Results.NotFound());
        a.MapGet("/payments/summary", async (InvoiceQueries q, CancellationToken ct) => Results.Ok(await q.SummaryAsync(ct)));
        var p = app.MapGroup("/api/public/invoices").RequireRateLimiting("public-payments").AddEndpointFilter<PaymentErrorFilter>();
        p.MapGet("", async (HttpContext c, InvoiceOperations ops, CancellationToken ct) => Results.Ok(await ops.ReviewAsync(Token(c), ct)));
        p.MapGet("/document", async (HttpContext c, InvoiceOperations ops, TravelAgencyDbContext db, CancellationToken ct) => { await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await ops.CustomerAsync(Token(c), ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Document(i); });
        p.MapPost("/payment-session", async (HttpContext c, PaymentProcessing payments, CancellationToken ct) => Results.Ok(await payments.SessionAsync(Token(c), ct)));
        app.MapPaymentWebhooks();
    }
    private static IResult Document(Invoice i) => Results.File(Encoding.UTF8.GetBytes(InvoiceDocument.Html(i)), "text/html; charset=utf-8", i.Number + ".html");
    private static string Token(HttpContext c) => c.Request.Headers["X-Invoice-Token"].ToString();
    private static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
