using AILogic.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AILogic.API.Middleware;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AgentUnavailableException exception)
        {
            logger.LogWarning(exception, "AI agent request failed.");
            await WriteProblemAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "AI service unavailable",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }
}
