using System.Security.Claims;
using TravelAgency.Application.Admin;

namespace TravelAgency.Api.Admin;

public static class AdminRequestEndpoints
{
    public sealed record StatusInput(string Status, Guid Version);
    public sealed record NoteInput(string? Content);
    public static void MapAdminRequests(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>();
        group.MapGet("/dashboard/summary", async (IAdminQueries queries, CancellationToken ct) => Results.Ok(await queries.DashboardAsync(ct)));
        group.MapGet("/travel-requests", async (IAdminQueries queries, int? page, int? pageSize, string? status, string? serviceType, string? search, string? sort, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        {
            var query = new RequestQuery(page ?? 1, pageSize ?? 20, status, serviceType, search, sort ?? "createdAt_desc", from, to);
            return query.IsValid ? Results.Ok(await queries.ListAsync(query, ct)) : Results.Problem(statusCode: 400, title: "Invalid filters or pagination.");
        });
        group.MapGet("/travel-requests/{id:guid}", async (Guid id, IAdminQueries queries, CancellationToken ct) => await queries.DetailAsync(id, ct) is { } detail ? Results.Ok(detail) : Results.NotFound());
        group.MapPatch("/travel-requests/{id:guid}/status", async (Guid id, StatusInput input, HttpContext context, IAdminMutations mutations, CancellationToken ct) => Outcome(await mutations.ChangeStatusAsync(id, input.Status, input.Version, context.User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct)));
        group.MapPost("/travel-requests/{id:guid}/notes", async (Guid id, NoteInput input, HttpContext context, IAdminMutations mutations, CancellationToken ct) => Outcome(await mutations.AddNoteAsync(id, input.Content, context.User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct)));
        group.MapGet("/travel-requests/{id:guid}/notes", async (Guid id, int? page, IAdminQueries queries, CancellationToken ct) =>
        {
            if (page is < 1 or > 100000) return Results.Problem(statusCode: 400, title: "Invalid page.");
            if (await queries.DetailAsync(id, ct) is null) return Results.NotFound();
            return Results.Ok(await queries.NotesAsync(id, page ?? 1, ct));
        });
        group.MapGet("/travel-requests/{id:guid}/activity", async (Guid id, int? page, IAdminQueries queries, CancellationToken ct) =>
        {
            if (page is < 1 or > 100000) return Results.Problem(statusCode: 400, title: "Invalid page.");
            if (await queries.DetailAsync(id, ct) is null) return Results.NotFound();
            return Results.Ok(await queries.ActivitiesAsync(id, page ?? 1, ct));
        });
    }
    private static IResult Outcome(MutationOutcome outcome) => outcome switch
    {
        MutationOutcome.Saved => Results.NoContent(),
        MutationOutcome.NotFound => Results.NotFound(),
        MutationOutcome.Conflict => Results.Problem(statusCode: 409, title: "The request changed. Refresh before retrying."),
        _ => Results.Problem(statusCode: 400, title: "Invalid status transition or note.")
    };
}
