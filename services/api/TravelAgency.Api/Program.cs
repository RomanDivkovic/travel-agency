using System.Text.Json;
using TravelAgency.Api.Health;
using TravelAgency.Api.Requests;
using TravelAgency.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TravelAgency")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__TravelAgency before starting the API.");

builder.Services.AddInfrastructure(connectionString);
builder.AddRequestFeature();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
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
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After");
        }
    }));
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
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

app.UseRateLimiter();
app.MapHealthEndpoints();
app.MapTravelRequestEndpoints();
app.Run();

public partial class Program;
