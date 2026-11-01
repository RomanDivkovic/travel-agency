using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Payments;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Api.Payments;

public static class PaymentEndpoints
{
    public sealed record SimulationInput(string Outcome, bool DeliverWebhook = true);
    public static void MapPaymentWebhooks(this WebApplication app)
    {
        app.MapPost("/api/payments/webhooks/{name}", async (string name, HttpContext c, IPaymentProvider provider, PaymentProcessing processing, CancellationToken ct) =>
        {
            if (name != provider.Name || name == "Disabled") return Results.NotFound();
            using var reader = new StreamReader(c.Request.Body); var body = await reader.ReadToEndAsync(ct);
            if (body.Length > 32768) return Results.StatusCode(413);
            VerifiedPaymentEvent evidence;
            try { evidence = provider.VerifyWebhook(new(body, c.Request.Headers["X-Payment-Signature"].ToString(), c.Request.Headers["X-Payment-Timestamp"].ToString())); }
            catch (PaymentProviderException) { return Results.Problem(statusCode: 400, title: "Invalid payment event."); }
            await processing.ApplyAsync(evidence, ct); return Results.NoContent();
        }).AddEndpointFilter<PaymentErrorFilter>();
        if (!app.Environment.IsDevelopment()) return;
        var sandbox = app.MapGroup("/api/public/payments/sandbox").RequireRateLimiting("public-payments").AddEndpointFilter<PaymentErrorFilter>();
        sandbox.MapGet("", async (HttpContext c, PaymentSettings settings, TravelAgencyDbContext db, InvoiceTokenCodec codec, TimeProvider clock, CancellationToken ct) =>
        {
            var s = await Access(c, settings, db, clock, ct); return Results.Ok(new { s.AmountMinorUnits, s.Currency, status = s.Status.ToString(), s.ExpiresAt, isSimulated = true, resultUrl = codec.Read(s.ProtectedReturnUrl) });
        });
        sandbox.MapPost("", async (HttpContext c, SimulationInput input, PaymentSettings settings, TravelAgencyDbContext db, SandboxPaymentProvider provider, PaymentProcessing processing, TimeProvider clock, CancellationToken ct) =>
        {
            if (input.Outcome is not ("Succeeded" or "Failed" or "Cancelled")) return Results.BadRequest();
            var access = await Access(c, settings, db, clock, ct); VerifiedPaymentEvent evidence;
            await using (var tx = await db.Database.BeginTransactionAsync(ct))
            {
                var s = (await db.SandboxPayments.FromSqlInterpolated($"SELECT * FROM \"SandboxPayments\" WHERE \"Id\" = {access.Id} FOR UPDATE").ToListAsync(ct)).Single();
                if (s.Status != PaymentStatus.Pending || s.ExpiresAt <= clock.GetUtcNow()) throw new InvoiceOperationException(409);
                s.Status = Enum.Parse<PaymentStatus>(input.Outcome); evidence = SandboxPaymentProvider.Evidence(s); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            }
            if (input.DeliverWebhook) await processing.ApplyAsync(provider.VerifyWebhook(provider.SignForSandbox(evidence)), ct);
            return Results.Ok(new { isSimulated = true, status = evidence.Status.ToString(), webhookDelivered = input.DeliverWebhook });
        });
    }
    private static async Task<SandboxPayment> Access(HttpContext c, PaymentSettings settings, TravelAgencyDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (settings.Provider != "Sandbox") throw new InvoiceOperationException(404);
        var hash = InvoiceTokenCodec.Hash(c.Request.Headers["X-Sandbox-Token"].ToString()); if (hash is null) throw new InvoiceOperationException(404);
        return await db.SandboxPayments.AsNoTracking().SingleOrDefaultAsync(s => s.AccessHash == hash && s.ExpiresAt > clock.GetUtcNow(), ct) ?? throw new InvoiceOperationException(404);
    }
}
