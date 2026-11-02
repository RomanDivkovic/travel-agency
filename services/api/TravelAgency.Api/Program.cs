using System.Text.Json;
using TravelAgency.Api.Admin;
using TravelAgency.Api.Bookings;
using TravelAgency.Api.Health;
using TravelAgency.Api.Payments;
using TravelAgency.Api.Quotations;
using TravelAgency.Api.Requests;
using TravelAgency.Api.Research;
using TravelAgency.Infrastructure;
using TravelAgency.Infrastructure.Admin;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TravelAgency")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__TravelAgency before starting the API.");

builder.Services.AddInfrastructure(connectionString);
builder.AddRequestFeature();
builder.AddAdminFeature();
builder.AddQuotations();
builder.AddPayments();
builder.AddResearch();
builder.AddBookings();
builder.Services.AddProblemDetails();
// Malformed minimal-API inputs are client errors, including in Development.
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"]);

if (builder.Environment.IsDevelopment())
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        || !uri.IsLoopback || (uri.Scheme != "http" && uri.Scheme != "https")))
    {
        throw new InvalidOperationException("Development CORS origins must be explicit loopback HTTP(S) origins.");
    }

    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    {
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After").AllowCredentials();
        }
    }));
}

var app = builder.Build();
if (args.Contains("--provision-admin"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var configuration = app.Configuration;
    var created = await scope.ServiceProvider.GetRequiredService<AdminProvisioner>().ProvisionAsync(
        configuration["Admin:Email"] ?? "", configuration["Admin:InitialPassword"], configuration["Admin:DisplayName"] ?? "Travel Agent", CancellationToken.None);
    Console.WriteLine(created ? "Administrator provisioned." : "Administrator already exists; no changes made.");
    return;
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    if (context.Request.Path.StartsWithSegments("/api/admin") || context.Request.Path.StartsWithSegments("/api/public") || context.Request.Path.StartsWithSegments("/api/payments")) context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseExceptionHandler(error => error.Run(async context =>
{
    await Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected error occurred.").ExecuteAsync(context);
}));
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapQuotations();
app.MapInvoices();
app.MapResearch();
app.MapBookings();
app.MapAdminAuth();
app.MapAdminRequests();
app.MapHealthEndpoints();
app.MapTravelRequestEndpoints();
app.Run();

public partial class Program;
