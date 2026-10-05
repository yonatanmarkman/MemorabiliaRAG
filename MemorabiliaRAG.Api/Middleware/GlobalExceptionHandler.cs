using System.ClientModel;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MemorabiliaRAG.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The client disconnected — nothing to send back.
        if (exception is OperationCanceledException 
            && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Path} was cancelled by the client.", httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        int statusCode;
        string title;
        string detail;

        // An error on OpenAI's side is considered bad gateway error.
        if (exception is ClientResultException clientException)
        {
            _logger.LogError(exception, "OpenAI request failed with status {Status} on {Path}.", clientException.Status, httpContext.Request.Path);
            statusCode = StatusCodes.Status502BadGateway;
            title = "AI service error";
            detail = "The AI service failed to process the request. Please try again later.";
        }
        else // Any other server side error is an internal error on our side.
        {
            _logger.LogError(exception, "Unhandled exception on {Path}.", httpContext.Request.Path);
            statusCode = StatusCodes.Status500InternalServerError;
            title = "Internal server error";
            detail = "An unexpected error occurred while processing your request.";
        }

        httpContext.Response.StatusCode = statusCode;

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
