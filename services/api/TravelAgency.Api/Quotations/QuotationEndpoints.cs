using System.Security.Claims;
using TravelAgency.Api.Admin;
using TravelAgency.Application.Quotations;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Api.Quotations;

public static class QuotationEndpoints
{
    public sealed record VersionInput(Guid VersionToken);
    public sealed record DecisionInput(bool Confirmed);
    public static void MapQuotations(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>();
        admin.MapGet("/quotations/configuration", (QuotationSettings settings) => Results.Ok(settings.ToDto()));
        admin.MapPost("/travel-requests/{requestId:guid}/quotations", async (Guid requestId, HttpContext c, IQuotationDrafts drafts, CancellationToken ct) => Result(await drafts.CreateAsync(requestId, Actor(c), ct), created: true));
        admin.MapGet("/travel-requests/{requestId:guid}/quotations", async (Guid requestId, int? page, IQuotationQueries queries, CancellationToken ct) => await List(queries, new(Page: page ?? 1, RequestId: requestId), ct));
        admin.MapGet("/quotations", async (int? page, int? pageSize, string? status, string? search, string? sort, IQuotationQueries queries, CancellationToken ct) => await List(queries, new(page ?? 1, pageSize ?? 20, status, search, sort ?? "createdAt_desc"), ct));
        admin.MapGet("/quotations/{id:guid}", async (Guid id, IQuotationQueries queries, CancellationToken ct) => await queries.DetailAsync(id, ct) is { } quote ? Results.Ok(quote) : Results.NotFound());
        admin.MapPatch("/quotations/{id:guid}", async (Guid id, QuotationDraftInput input, HttpContext c, IQuotationDrafts drafts, CancellationToken ct) => Result(await drafts.UpdateAsync(id, input, Actor(c), ct)));
        admin.MapPost("/quotations/{id:guid}/send", async (Guid id, VersionInput input, HttpContext c, IQuotationIssuance issuance, CancellationToken ct) => input.VersionToken == Guid.Empty ? Result(new(QuotationOutcome.Invalid)) : Result(await issuance.SendAsync(id, input.VersionToken, Actor(c), ct)));
        admin.MapPost("/quotations/{id:guid}/revise", async (Guid id, VersionInput input, HttpContext c, IQuotationDrafts drafts, CancellationToken ct) => input.VersionToken == Guid.Empty ? Result(new(QuotationOutcome.Invalid)) : Result(await drafts.ReviseAsync(id, input.VersionToken, Actor(c), ct), created: true));
        admin.MapPost("/quotations/{id:guid}/cancel", async (Guid id, VersionInput input, HttpContext c, IQuotationIssuance issuance, CancellationToken ct) => input.VersionToken == Guid.Empty ? Result(new(QuotationOutcome.Invalid)) : Result(await issuance.CancelAsync(id, input.VersionToken, Actor(c), ct)));
        admin.MapGet("/quotations/{id:guid}/activity", async (Guid id, int? page, IQuotationQueries queries, CancellationToken ct) =>
        {
            if (page is < 1 or > 100000) return Results.Problem(statusCode: 400, title: "Invalid page.");
            return await queries.DetailAsync(id, ct) is null ? Results.NotFound() : Results.Ok(await queries.HistoryAsync(id, page ?? 1, ct));
        });
        var customer = app.MapGroup("/api/public/quotations").RequireRateLimiting("public-quotations");
        customer.MapGet("", async (HttpContext c, IQuotationCustomers customers, CancellationToken ct) => Result(await customers.ReviewAsync(Token(c), ct)));
        customer.MapPost("/accept", async (HttpContext c, DecisionInput input, IQuotationCustomers customers, CancellationToken ct) => Result(await customers.RespondAsync(Token(c), true, input.Confirmed, ct)));
        customer.MapPost("/decline", async (HttpContext c, DecisionInput input, IQuotationCustomers customers, CancellationToken ct) => Result(await customers.RespondAsync(Token(c), false, input.Confirmed, ct)));
    }
    private static string Actor(HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static string Token(HttpContext context) => context.Request.Headers["X-Quotation-Token"].ToString();
    private static async Task<IResult> List(IQuotationQueries queries, QuotationQuery query, CancellationToken ct) => query.IsValid ? Results.Ok(await queries.ListAsync(query, ct)) : Results.Problem(statusCode: 400, title: "Invalid quotation filters.");
    private static IResult Result(QuotationResult result, bool created = false) => result.Outcome switch
    {
        QuotationOutcome.Saved when result.Quotation is not null => Results.Ok(result.Quotation),
        QuotationOutcome.Saved when created => Results.Json(new { id = result.Id }, statusCode: 201),
        QuotationOutcome.Saved => Results.NoContent(),
        QuotationOutcome.NotFound => Results.Problem(statusCode: 404, title: "Quotation unavailable."),
        QuotationOutcome.Conflict => Results.Problem(statusCode: 409, title: "The quotation changed or can no longer be acted on. Refresh before retrying."),
        QuotationOutcome.IssuanceBlocked => Results.Problem(statusCode: 412, title: "Issuance requires configured approval of tax treatment, terms and acceptance wording."),
        _ => Results.Problem(statusCode: 400, title: "Check the quotation fields and explicit confirmation.")
    };
}
