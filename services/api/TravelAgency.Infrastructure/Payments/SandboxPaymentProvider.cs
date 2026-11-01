using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Application.Payments;
using TravelAgency.Domain.Payments;
using TravelAgency.Infrastructure.Persistence;

namespace TravelAgency.Infrastructure.Payments;

// Deliberately separate provider evidence from application payment state. Development only.
public sealed class SandboxPayment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string SessionId { get; set; } = "";
    public string AccessHash { get; set; } = "";
    public string ProtectedAccess { get; set; } = "";
    public string ProtectedReturnUrl { get; set; } = "";
    public long AmountMinorUnits { get; set; }
    public string Currency { get; set; } = "";
    public PaymentStatus Status { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
public sealed class SandboxPaymentProvider(TravelAgencyDbContext db, PaymentSettings settings, InvoiceTokenCodec codec, TimeProvider clock) : IPaymentProvider
{
    public string Name => "Sandbox";
    public bool IsSimulated => true;
    public async Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest r, CancellationToken ct)
    {
        var existing = await db.SandboxPayments.SingleOrDefaultAsync(s => s.Id == r.AttemptId, ct);
        if (existing is null)
        {
            var credential = InvoiceTokenCodec.Create();
            existing = new SandboxPayment
            {
                Id = r.AttemptId,
                InvoiceId = r.InvoiceId,
                SessionId = "sandbox_" + r.AttemptId.ToString("N"),
                AmountMinorUnits = r.AmountMinorUnits,
                Currency = r.Currency,
                AccessHash = InvoiceTokenCodec.Hash(credential)!,
                ProtectedAccess = codec.Protect(credential),
                ProtectedReturnUrl = codec.Protect(r.ReturnUrl),
                ExpiresAt = clock.GetUtcNow().AddMinutes(30)
            };
            db.SandboxPayments.Add(existing);
        }
        if (existing.InvoiceId != r.InvoiceId || existing.AmountMinorUnits != r.AmountMinorUnits || existing.Currency != r.Currency) throw new PaymentProviderException();
        return new(existing.SessionId, settings.PublicOrigin + "/payment/sandbox#" + codec.Read(existing.ProtectedAccess), existing.ExpiresAt);
    }
    public VerifiedPaymentEvent VerifyWebhook(PaymentWebhookRequest r)
    {
        if (!long.TryParse(r.Timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || Math.Abs(clock.GetUtcNow().ToUnixTimeSeconds() - (decimal)seconds) > 300) throw new PaymentProviderException();
        byte[] signature;
        try { signature = Convert.FromHexString(r.Signature); } catch (FormatException) { throw new PaymentProviderException(); }
        if (!CryptographicOperations.FixedTimeEquals(Sign(r.Timestamp, r.Body), signature)) throw new PaymentProviderException();
        VerifiedPaymentEvent? result;
        try { result = JsonSerializer.Deserialize<VerifiedPaymentEvent>(r.Body); } catch (JsonException) { throw new PaymentProviderException(); }
        if (result is null || result.Merchant != "development-simulator" || result.EventId is not { Length: > 0 and <= 100 } || result.SessionId is not { Length: > 0 and <= 100 }
            || result.PaymentId is not { Length: > 0 and <= 100 } || !Enum.IsDefined(result.Status)) throw new PaymentProviderException();
        return result;
    }
    private byte[] Sign(string timestamp, string body) => HMACSHA256.HashData(Encoding.UTF8.GetBytes(settings.SandboxSigningSecret), Encoding.UTF8.GetBytes(timestamp + "." + body));
    public PaymentWebhookRequest SignForSandbox(VerifiedPaymentEvent evidence)
    {
        var body = JsonSerializer.Serialize(evidence); var timestamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return new(body, Convert.ToHexString(Sign(timestamp, body)), timestamp);
    }
    public async Task<VerifiedPaymentEvent?> ReconcileAsync(string sessionId, CancellationToken ct)
    {
        var s = await db.SandboxPayments.AsNoTracking().SingleOrDefaultAsync(s => s.SessionId == sessionId, ct);
        return s is null ? null : Evidence(s);
    }
    public static VerifiedPaymentEvent Evidence(SandboxPayment s) => new("sandbox-" + s.Id.ToString("N") + "-" + s.Status, s.SessionId, "simulated-" + s.Id.ToString("N"), s.InvoiceId,
        s.AmountMinorUnits, s.Currency, s.Status, "development-simulator");
}
public sealed class DisabledPaymentProvider : IPaymentProvider
{
    public string Name => "Disabled";
    public bool IsSimulated => false;
    public Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken ct) => throw new PaymentProviderException();
    public VerifiedPaymentEvent VerifyWebhook(PaymentWebhookRequest request) => throw new PaymentProviderException();
    public Task<VerifiedPaymentEvent?> ReconcileAsync(string sessionId, CancellationToken ct) => Task.FromResult<VerifiedPaymentEvent?>(null);
}
