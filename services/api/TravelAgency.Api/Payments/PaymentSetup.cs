using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Application.Payments;
using TravelAgency.Infrastructure.Payments;

namespace TravelAgency.Api.Payments;

public static class PaymentSetup
{
    public static void AddPayments(this WebApplicationBuilder b)
    {
        var s = b.Configuration.GetSection("Payments").Get<PaymentSettings>() ?? new();
        if (s.Provider is not ("Disabled" or "Sandbox") || s.AccessDays is < 1 or > 366 || new[] { s.SellerName, s.SellerAddress, s.RegistrationNumber, s.TaxInformation, s.TermsVersion }.Any(x => x.Length > 1000) || s.TermsSv.Length > 6000 || s.TermsEn.Length > 6000) throw new InvalidOperationException("Invalid payment settings.");
        if (s.Provider == "Sandbox" && (!b.Environment.IsDevelopment() || s.SandboxSigningSecret.Length < 32)) throw new InvalidOperationException("Sandbox payments require Development and a signing secret of at least 32 characters.");
        if (s.PublicOrigin.Length > 0)
        {
            if (!Uri.TryCreate(s.PublicOrigin, UriKind.Absolute, out var origin) || origin.Scheme is not ("http" or "https") || origin.UserInfo.Length > 0 || origin.AbsolutePath != "/" || origin.Query.Length > 0 || origin.Fragment.Length > 0
                || (origin.Scheme == "http" && (!origin.IsLoopback || !b.Environment.IsDevelopment()))) throw new InvalidOperationException("Payment origin must be HTTPS, or loopback HTTP in Development.");
            s.PublicOrigin = origin.GetLeftPart(UriPartial.Authority);
        }
        b.Services.AddSingleton(s); b.Services.AddScoped<InvoiceTokenCodec>(); b.Services.AddScoped<InvoiceOperations>(); b.Services.AddScoped<InvoiceQueries>(); b.Services.AddScoped<PaymentProcessing>(); b.Services.AddScoped<PaymentMaintenance>();
        b.Services.AddScoped<SandboxPaymentProvider>(); b.Services.AddScoped<IPaymentProvider>(sp => s.Provider == "Sandbox" ? sp.GetRequiredService<SandboxPaymentProvider>() : new DisabledPaymentProvider());
        b.Services.AddRateLimiter(o => o.AddPolicy("public-payments", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
    }
}
public sealed class PaymentErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (InvoiceOperationException e) { return Results.Problem(statusCode: e.Status, title: e.Status switch { 404 => "Invoice or payment unavailable.", 409 => "Invoice state changed or payment evidence did not match. Refresh before retrying.", 412 => "Invoice issuance requires approved business, accounting, tax and provider configuration.", _ => "Check invoice details." }); }
        catch (TravelAgency.Domain.Payments.InvoiceRuleException) { return Results.Problem(statusCode: 400, title: "Check invoice details and permitted transitions."); }
        catch (PaymentProviderException) { return Results.Problem(statusCode: 503, title: "Payment provider unavailable or verification failed."); }
    }
}
