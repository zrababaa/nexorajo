using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SMPP.Application.Common;
using SMPP.Web.Api;

namespace SMPP.Web.Filters;

/// <summary>
/// Turns the application layer's exceptions into API responses. The MVC screens catch
/// <see cref="AppException"/> themselves to re-render a form; the API has no form to re-render,
/// so it is translated here once instead of in every action:
/// a rejected business rule is a 400, a message stopped by the content filter is a 422 carrying
/// the offending terms, and anything unexpected is a 500 that says nothing about internals.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        // MVC controllers keep their own error handling (flash messages, re-rendered views).
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        switch (context.Exception)
        {
            case SpamBlockedException spam:
                context.Result = new ObjectResult(new ApiErrorResponse(spam.Message) { BlockedTerms = spam.MatchedTerms })
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity,
                };
                break;

            case AppException app:
                context.Result = new BadRequestObjectResult(new ApiErrorResponse(app.Message));
                break;

            default:
                _logger.LogError(context.Exception, "Unhandled exception on {Path}", context.HttpContext.Request.Path);
                context.Result = new ObjectResult(new ApiErrorResponse(
                    _environment.IsDevelopment() ? context.Exception.Message : "An unexpected error occurred."))
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                };
                break;
        }

        context.ExceptionHandled = true;
    }
}
