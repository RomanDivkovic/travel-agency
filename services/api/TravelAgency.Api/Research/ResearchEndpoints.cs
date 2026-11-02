using System.Security.Claims;
using TravelAgency.Api.Admin;
using TravelAgency.Application.Research;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Api.Research;

public static class ResearchEndpoints
{
    public sealed record NotesInput(Guid VersionToken, string? Notes);
    public sealed record VersionInput(Guid VersionToken);
    public sealed record OptionInput(Guid VersionToken, ResearchOptionInput Option);
    public static void MapResearch(this WebApplication app)
    {
        var a = app.MapGroup("/api/admin").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>().AddEndpointFilter<ResearchErrorFilter>();
        a.MapGet("/travel-requests/{id:guid}/research", async (Guid id, int? page, ResearchOperations ops, CancellationToken ct) => Results.Ok(await ops.GetAsync(id, page ?? 1, ct)));
        a.MapPost("/travel-requests/{id:guid}/research/start", async (Guid id, HttpContext c, ResearchOperations ops, CancellationToken ct) => { await ops.StartAsync(id, Actor(c), ct); return Results.NoContent(); });
        a.MapPatch("/travel-requests/{id:guid}/research", async (Guid id, NotesInput input, HttpContext c, ResearchOperations ops, CancellationToken ct) => { await ops.SaveNotesAsync(id, input.VersionToken, input.Notes, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/travel-requests/{id:guid}/research/options", async (Guid id, ResearchOptionInput input, HttpContext c, ResearchOperations ops, CancellationToken ct) => Results.Json(new { id = await ops.SaveOptionAsync(id, null, null, input, Actor(c), ct) }, statusCode: 201));
        a.MapPatch("/travel-requests/{id:guid}/research/options/{optionId:guid}", async (Guid id, Guid optionId, OptionInput input, HttpContext c, ResearchOperations ops, CancellationToken ct) => { await ops.SaveOptionAsync(id, optionId, input.VersionToken, input.Option, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/travel-requests/{id:guid}/research/options/{optionId:guid}/remove", async (Guid id, Guid optionId, VersionInput input, HttpContext c, ResearchOperations ops, CancellationToken ct) => { await ops.RemoveOptionAsync(id, optionId, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        a.MapGet("/proposals/configuration", (ProposalSettings s) => Results.Ok(new { blockingReasons = s.BlockingReasons(), s.TermsSv, s.TermsEn }));
        a.MapGet("/proposals", async (int? page, string? status, string? search, ProposalQueries q, CancellationToken ct) => Results.Ok(await q.ListAsync(page ?? 1, status, search, null, ct)));
        a.MapPost("/travel-requests/{id:guid}/proposals", async (Guid id, HttpContext c, ProposalDrafts drafts, CancellationToken ct) => Results.Json(new { id = await drafts.CreateAsync(id, Actor(c), ct) }, statusCode: 201));
        a.MapGet("/proposals/{id:guid}", async (Guid id, ProposalQueries q, CancellationToken ct) => await q.DetailAsync(id, ct) is { } p ? Results.Ok(p) : Results.NotFound());
        a.MapGet("/proposals/{id:guid}/activity", async (Guid id, int? page, ProposalQueries q, CancellationToken ct) => Results.Ok(await q.HistoryAsync(id, page ?? 1, ct)));
        a.MapPatch("/proposals/{id:guid}", async (Guid id, ProposalDraftInput input, HttpContext c, ProposalDrafts drafts, CancellationToken ct) => { await drafts.UpdateAsync(id, input, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/proposals/{id:guid}/send", async (Guid id, VersionInput input, HttpContext c, ProposalIssuance issuance, CancellationToken ct) => { await issuance.SendAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/proposals/{id:guid}/revise", async (Guid id, VersionInput input, HttpContext c, ProposalDrafts drafts, CancellationToken ct) => Results.Json(new { id = await drafts.ReviseAsync(id, input.VersionToken, Actor(c), ct) }, statusCode: 201));
        a.MapPost("/proposals/{id:guid}/revoke-access", async (Guid id, VersionInput input, HttpContext c, ProposalIssuance issuance, CancellationToken ct) => { await issuance.RevokeAccessAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        a.MapPost("/proposals/{id:guid}/cancel", async (Guid id, VersionInput input, HttpContext c, ProposalIssuance issuance, CancellationToken ct) => { await issuance.CancelAsync(id, input.VersionToken, Actor(c), ct); return Results.NoContent(); });
        var p = app.MapGroup("/api/public/travel-proposals").RequireRateLimiting("public-proposals").AddEndpointFilter<ResearchErrorFilter>();
        p.MapGet("", async (HttpContext c, ProposalCustomers customers, CancellationToken ct) => Results.Ok(await customers.ExecuteAsync(Token(c), null, null, ct)));
        p.MapPost("/approve", async (HttpContext c, ProposalDecisionInput input, ProposalCustomers customers, CancellationToken ct) => Results.Ok(await customers.ExecuteAsync(Token(c), true, input, ct)));
        p.MapPost("/reject", async (HttpContext c, ProposalDecisionInput input, ProposalCustomers customers, CancellationToken ct) => Results.Ok(await customers.ExecuteAsync(Token(c), false, input, ct)));
    }
    private static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static string Token(HttpContext c) => c.Request.Headers["X-Proposal-Token"].ToString();
}
