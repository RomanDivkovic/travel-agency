using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Research;

namespace TravelAgency.Api.Research;

public static class ResearchSetup
{
    public static void AddResearch(this WebApplicationBuilder b)
    {
        var s = b.Configuration.GetSection("Proposals").Get<ProposalSettings>() ?? new();
        if (s.AccessRetentionDays is < 1 or > 90 || s.DefaultValidityDays is < 1 or > 90 || s.TermsVersion.Length > 100 || s.TermsSv.Length > 6000 || s.TermsEn.Length > 6000 || s.AcceptanceTextSv.Length > 2000 || s.AcceptanceTextEn.Length > 2000) throw new InvalidOperationException("Invalid proposal configuration.");
        if (s.PublicOrigin.Length > 0)
        {
            if (!Uri.TryCreate(s.PublicOrigin, UriKind.Absolute, out var origin) || origin.Scheme is not ("http" or "https") || origin.UserInfo.Length > 0 || origin.AbsolutePath != "/" || origin.Query.Length > 0 || origin.Fragment.Length > 0 || (origin.Scheme == "http" && (!origin.IsLoopback || !b.Environment.IsDevelopment()))) throw new InvalidOperationException("Proposal origin requires HTTPS (loopback HTTP in Development only).");
            s.PublicOrigin = origin.GetLeftPart(UriPartial.Authority);
        }
        b.Services.AddSingleton(s); b.Services.AddScoped<ProposalTokenCodec>(); b.Services.AddScoped<ResearchTransaction>(); b.Services.AddScoped<ResearchOperations>(); b.Services.AddScoped<ProposalQueries>(); b.Services.AddScoped<ProposalDrafts>(); b.Services.AddScoped<ProposalIssuance>(); b.Services.AddScoped<ProposalCustomers>(); b.Services.AddScoped<ProposalMaintenance>();
        b.Services.AddRateLimiter(o => o.AddPolicy("public-proposals", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
    }
}
public sealed class ResearchErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (ResearchOperationException e) { return Results.Problem(statusCode: e.Status, title: e.Status switch { 403 => "Verified real service payment is required for research.", 404 => "Research item or proposal unavailable.", 409 => "State changed or this operation is unavailable. Refresh before retrying.", 412 => "Sending requires reviewed proposal terms and approved configuration.", _ => "Check the research or proposal fields." }); }
        catch (ResearchRuleException) { return Results.Problem(statusCode: 400, title: "Check research details, timezone offsets, selection and permitted transitions."); }
    }
}
