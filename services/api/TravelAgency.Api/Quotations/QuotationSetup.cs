using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Application.Quotations;
using TravelAgency.Domain.Quotations;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Api.Quotations;

public static partial class QuotationSetup
{
    public static void AddQuotations(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetSection("Quotations").Get<QuotationSettings>() ?? new();
        if (!Prefix().IsMatch(settings.ReferencePrefix) || settings.DefaultValidityDays is < 1 or > 90 || settings.AccessRetentionDays is < 1 or > 90
            || settings.PublicPermitLimit is < 1 or > 1000 || settings.TermsVersion.Length > 100 || settings.TermsSv.Length > 6000 || settings.TermsEn.Length > 6000
            || settings.AcceptanceTextSv.Length > 2000 || settings.AcceptanceTextEn.Length > 2000
            || settings.ApprovedTaxTreatments.Any(t => !Enum.TryParse<TaxTreatment>(t, false, out var mode) || !Enum.IsDefined(mode) || mode == TaxTreatment.Unconfirmed || mode.ToString() != t)
            || settings.ApprovedTaxRatesBasisPoints.Any(r => r is < 0 or > 10000)) throw new InvalidOperationException("Invalid quotation configuration.");
        if (!string.IsNullOrEmpty(settings.PublicOrigin))
        {
            if (!Uri.TryCreate(settings.PublicOrigin, UriKind.Absolute, out var origin) || origin.Scheme is not ("http" or "https") || origin.UserInfo.Length != 0
                || origin.Query.Length != 0 || origin.Fragment.Length != 0 || origin.AbsolutePath != "/" || (!builder.Environment.IsDevelopment() && origin.Scheme != "https")
                || (origin.Scheme == "http" && !origin.IsLoopback)) throw new InvalidOperationException("Quotation public origin must be an approved HTTPS origin (loopback HTTP in Development only).");
            settings.PublicOrigin = origin.GetLeftPart(UriPartial.Authority);
        }
        builder.Services.AddSingleton(settings); builder.Services.AddScoped<QuotationTokenCodec>(); builder.Services.AddScoped<QuotationTransaction>();
        builder.Services.AddScoped<IQuotationDrafts, QuotationDrafts>(); builder.Services.AddScoped<IQuotationIssuance, QuotationIssuance>();
        builder.Services.AddScoped<IQuotationCustomers, QuotationCustomers>(); builder.Services.AddScoped<IQuotationQueries, QuotationQueries>(); builder.Services.AddScoped<IQuotationExpiration, QuotationExpiration>();
        builder.Services.AddRateLimiter(options => options.AddPolicy("public-quotations", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = settings.PublicPermitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
    }
    [GeneratedRegex("^[A-Z]{2,8}$")]
    private static partial Regex Prefix();
}
