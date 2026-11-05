using System.Text;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Api.Payments;
using TravelAgency.Api.Research;
using TravelAgency.Application.Quotations;
using TravelAgency.Application.Research;
using TravelAgency.Infrastructure.Customers;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Api.Customers;

public static class CustomerPortalEndpoints
{
    public sealed record DecisionInput(bool Confirmed);
    public static void MapCustomerPortal(this WebApplication app)
    {
        var a = app.MapGroup("/api/customer").RequireAuthorization(CustomerSetup.Policy).AddEndpointFilter<CustomerCsrfFilter>().AddEndpointFilter<CustomerErrorFilter>().AddEndpointFilter<ResearchErrorFilter>().AddEndpointFilter<PaymentErrorFilter>();
        a.MapGet("/dashboard", async (HttpContext c, CustomerPortalQueries q, CancellationToken ct) => Results.Ok(await q.DashboardAsync(CustomerAuthEndpoints.Actor(c), ct)));
        foreach (var kind in new[] { "requests", "quotations", "invoices", "proposals", "bookings" })
        {
            var resource = kind;
            a.MapGet("/" + (kind == "requests" ? "travel-requests" : kind), async (int? page, string? status, string? sort, Guid? requestId, HttpContext c, CustomerPortalQueries q, CancellationToken ct) => Results.Ok(await q.ListAsync(CustomerAuthEndpoints.Actor(c), resource, page ?? 1, status, sort, requestId, ct)));
        }
        a.MapGet("/travel-requests/{id:guid}", async (Guid id, HttpContext c, CustomerPortalQueries q, CancellationToken ct) =>
        {
            var actor = CustomerAuthEndpoints.Actor(c); var request = await q.RequestAsync(actor, id, ct); var related = new List<Application.Customers.PortalItem>();
            foreach (var kind in new[] { "quotations", "invoices", "proposals", "bookings" }) related.AddRange((await q.ListAsync(actor, kind, 1, null, null, id, ct)).Items);
            return Results.Ok(new { request, related });
        });
        a.MapGet("/activity", async (Guid? requestId, int? page, HttpContext c, CustomerPortalQueries q, CancellationToken ct) => Results.Ok(await q.ActivityAsync(CustomerAuthEndpoints.Actor(c), requestId, page ?? 1, ct)));
        a.MapGet("/quotations/{id:guid}", async (Guid id, HttpContext c, IQuotationCustomers quotes, CancellationToken ct) => QuoteResult(await quotes.OwnedAsync(CustomerAuthEndpoints.Actor(c), id, null, true, ct)));
        foreach (var action in new[] { "accept", "decline" }) { var accept = action == "accept"; a.MapPost("/quotations/{id:guid}/" + action, async (Guid id, DecisionInput input, HttpContext c, IQuotationCustomers quotes, CancellationToken ct) => QuoteResult(await quotes.OwnedAsync(CustomerAuthEndpoints.Actor(c), id, accept, input.Confirmed, ct))); }
        a.MapGet("/invoices/{id:guid}", async (Guid id, HttpContext c, InvoiceOperations invoices, CancellationToken ct) => Results.Ok(await invoices.ReviewOwnedAsync(id, CustomerAuthEndpoints.Actor(c), ct)));
        a.MapPost("/invoices/{id:guid}/payment-session", async (Guid id, HttpContext c, PaymentProcessing payments, CancellationToken ct) => Results.Ok(await payments.SessionOwnedAsync(id, CustomerAuthEndpoints.Actor(c), ct)));
        a.MapGet("/invoices/{id:guid}/document", async (Guid id, HttpContext c, InvoiceOperations invoices, TravelAgencyDbContext db, CancellationToken ct) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct); var i = await invoices.OwnedAsync(id, CustomerAuthEndpoints.Actor(c), ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.File(Encoding.UTF8.GetBytes(InvoiceDocument.Html(i)), "text/html; charset=utf-8", i.Number + ".html");
        });
        a.MapGet("/proposals/{id:guid}", async (Guid id, HttpContext c, ProposalCustomers proposals, CancellationToken ct) => Results.Ok(await proposals.OwnedAsync(id, CustomerAuthEndpoints.Actor(c), null, null, ct)));
        foreach (var action in new[] { "approve", "reject" }) { var approve = action == "approve"; a.MapPost("/proposals/{id:guid}/" + action, async (Guid id, ProposalDecisionInput input, HttpContext c, ProposalCustomers proposals, CancellationToken ct) => Results.Ok(await proposals.OwnedAsync(id, CustomerAuthEndpoints.Actor(c), approve, input, ct))); }
        a.MapGet("/bookings/{id:guid}", async (Guid id, HttpContext c, CustomerPortalQueries q, CancellationToken ct) => Results.Ok(await q.BookingAsync(CustomerAuthEndpoints.Actor(c), id, ct)));
    }
    private static IResult QuoteResult(QuotationResult r) => r.Outcome == QuotationOutcome.Saved ? Results.Ok(r.Quotation) : Results.Problem(statusCode: r.Outcome switch { QuotationOutcome.NotFound => 404, QuotationOutcome.Conflict => 409, _ => 400 }, title: "Quotation unavailable, changed or confirmation required.");
}
