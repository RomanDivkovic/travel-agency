using System.Security.Claims;
using TravelAgency.Api.Admin;
using TravelAgency.Api.Customers;
using TravelAgency.Application.Messaging;
using TravelAgency.Infrastructure.Messaging;
namespace TravelAgency.Api.Messaging;

public static class MessagingEndpoints
{
    public static void MapMessaging(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/conversations").RequireAuthorization(AdminSetup.Policy).AddEndpointFilter<AdminCsrfFilter>().AddEndpointFilter<MessagingErrorFilter>();
        var customer = app.MapGroup("/api/customer/conversations").RequireAuthorization(CustomerSetup.Policy).AddEndpointFilter<CustomerCsrfFilter>().AddEndpointFilter<MessagingErrorFilter>().AddEndpointFilter<CustomerErrorFilter>();
        customer.MapGet("/configuration", (MessagingSettings s) => Results.Ok(new { maxMessageLength = s.MaxMessageLength }));
        customer.MapGet("/links", async (string kind, int? page, string? search, Guid? id, HttpContext c, MessagingQueries q, CancellationToken ct) => Results.Ok(await q.LinksAsync(Actor(c), kind, page ?? 1, search, id, ct)));
        customer.MapPost("", async (NewConversation input, HttpContext c, MessagingOperations ops, CancellationToken ct) => Results.Json(new { id = await ops.CreateAsync(Actor(c), input, ct) }, statusCode: 201)).RequireRateLimiting("message-submissions");
        customer.MapPatch("/{id:guid}/notifications", async (Guid id, ConversationPreferences input, HttpContext c, MessagingOperations ops, CancellationToken ct) => { await ops.ReadAsync(id, Actor(c), false, 0, input.EmailNotifications, ct); return Results.NoContent(); });
        foreach (var (group, isAdmin) in new[] { (admin, true), (customer, false) })
        {
            var a = isAdmin;
            group.MapGet("", async (int? page, string? status, string? priority, string? search, bool? unread, Guid? requestId, Guid? bookingId, HttpContext c, MessagingQueries q, CancellationToken ct) => Results.Ok(await q.ListAsync(Actor(c), a, page ?? 1, status, priority, search, unread ?? false, requestId, bookingId, ct)));
            group.MapGet("/unread-count", async (HttpContext c, MessagingQueries q, CancellationToken ct) => Results.Ok(await q.SummaryAsync(Actor(c), a, ct)));
            group.MapGet("/{id:guid}", async (Guid id, long? before, HttpContext c, MessagingQueries q, CancellationToken ct) => Results.Ok(await q.DetailAsync(id, Actor(c), a, before, ct)));
            group.MapPost("/{id:guid}/messages", async (Guid id, SendMessage input, HttpContext c, MessagingOperations ops, CancellationToken ct) => Results.Json(new { id = await ops.SendAsync(id, Actor(c), a, input, ct) }, statusCode: 201)).RequireRateLimiting("message-submissions");
            group.MapPost("/{id:guid}/read", async (Guid id, ConversationRead input, HttpContext c, MessagingOperations ops, CancellationToken ct) => { await ops.ReadAsync(id, Actor(c), a, input.ThroughSequence, null, ct); return Results.NoContent(); });
        }
        foreach (var name in new[] { "status", "priority" }) { var operation = name; admin.MapPatch("/{id:guid}/" + operation, async (Guid id, ConversationChange input, HttpContext c, MessagingOperations ops, CancellationToken ct) => { await ops.ChangeAsync(id, Actor(c), operation, input, ct); return Results.NoContent(); }); }
    }
    private static string Actor(HttpContext c) => c.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
