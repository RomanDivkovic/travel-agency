using Microsoft.AspNetCore.Antiforgery;

namespace TravelAgency.Api.Admin;

public sealed class AdminCsrfFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
            catch (AntiforgeryValidationException) { return Results.Problem(statusCode: 400, title: "Security token invalid. Refresh and retry."); }
        }
        return await next(context);
    }
}
