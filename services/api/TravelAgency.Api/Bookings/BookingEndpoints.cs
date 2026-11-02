using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Api.Admin;
using TravelAgency.Api.Research;
using TravelAgency.Application.Bookings;
using TravelAgency.Domain.Bookings;
using TravelAgency.Infrastructure.Bookings;

namespace TravelAgency.Api.Bookings;

public static class BookingEndpoints
{
    public static void AddBookings(this WebApplicationBuilder b) { b.Services.AddScoped<BookingOperations>(); b.Services.AddScoped<BookingQueries>(); }
    public static void MapBookings(this WebApplication app)
    {
        var a = app.MapGroup("/api/admin").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>().AddEndpointFilter<BookingErrorFilter>().AddEndpointFilter<ResearchErrorFilter>();
        a.MapGet("/bookings", async (int? page, string? status, string? search, string? sort, Guid? requestId, BookingQueries q, CancellationToken ct) => Results.Ok(await q.ListAsync(page ?? 1, status, search, sort, requestId, ct)));
        a.MapGet("/bookings/metrics", async (BookingQueries q, CancellationToken ct) => Results.Ok(await q.MetricsAsync(ct)));
        a.MapGet("/bookings/{id:guid}", async (Guid id, BookingQueries q, CancellationToken ct) => Results.Ok(await q.DetailAsync(id, ct)));
        a.MapGet("/bookings/{id:guid}/activity", async (Guid id, int? page, BookingQueries q, CancellationToken ct) => Results.Ok(await q.HistoryAsync(id, page ?? 1, ct)));
        a.MapPost("/proposals/{id:guid}/bookings", async (Guid id, CreateBookingInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => Results.Json(new { id = await ops.CreateAsync(id, input, Actor(c), ct) }, statusCode: 201));
        a.MapPatch("/bookings/{id:guid}", async (Guid id, BookingNotesInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, null, input.VersionToken, "notes", input, Actor(c), ct); return Results.NoContent(); });
        a.MapPatch("/bookings/{id:guid}/reservations/{rid:guid}", async (Guid id, Guid rid, ReservationUpdateInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, rid, input.VersionToken, "update", input, Actor(c), ct); return Results.NoContent(); });
        foreach (var action in new[] { "start", "await" }) { var op = action; a.MapPost("/bookings/{id:guid}/reservations/{rid:guid}/" + op, async (Guid id, Guid rid, BookingVersionInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, rid, input.VersionToken, op, input, Actor(c), ct); return Results.NoContent(); }); }
        a.MapPost("/bookings/{id:guid}/reservations/{rid:guid}/confirm", async (Guid id, Guid rid, ConfirmationInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, rid, input.VersionToken, "confirm", input, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/bookings/{id:guid}/reservations/{rid:guid}/reapprove", async (Guid id, Guid rid, ReapprovalInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, rid, input.VersionToken, "reapprove", input, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/bookings/{id:guid}/reservations/{rid:guid}/fail", async (Guid id, Guid rid, FailureInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, rid, input.VersionToken, "fail", input, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/bookings/{id:guid}/reservations/{rid:guid}/cancel", async (Guid id, Guid rid, CancellationInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, rid, input.VersionToken, "cancel", input, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/bookings/{id:guid}/send-confirmation", async (Guid id, BookingVersionInput input, HttpContext c, BookingOperations ops, CancellationToken ct) => { await ops.SendAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
    }
    private static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
public sealed class BookingErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (BookingRuleException) { return Results.Problem(statusCode: 400, title: "Check supplier evidence, current approval, payment arrangement and permitted transition."); }
        catch (BookingOperationException e) { return Results.Problem(statusCode: e.Status, title: e.Status == 409 ? "Booking changed or prerequisites are not met. Refresh and review." : "Booking unavailable or fields invalid."); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Booking changed. Refresh before retrying."); }
    }
}
