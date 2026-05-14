namespace MeetingRoom.Api.Middleware;

using System.Net;
using System.Text.Json;
using Core.Domain.Exceptions;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
        {
            logger.LogWarning(ex, "Calendar provider not configured");
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Calendar provider not configured. Upload credentials first." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal server error" }));
        }
    }
}
