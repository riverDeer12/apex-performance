namespace ApexPerformance.API.Middlewares;

/// <summary>
/// A request the client gave up on (closed the app, left the screen, lost the connection)
/// ends with whatever the cancelled work threw, often a SQL "Operation cancelled by user".
/// That is not an error of the application: it is logged as information instead of an
/// unhandled exception, so it doesn't end up in the error alerts.
/// </summary>
public class CancelledRequests
{
    // Not an HTTP status, but what nginx and others log for "client closed the request".
    private const int ClientClosedRequest = 499;

    private readonly RequestDelegate _next;
    private readonly ILogger<CancelledRequests> _logger;

    public CancelledRequests(RequestDelegate next, ILogger<CancelledRequests> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client ({Exception})",
                context.Request.Method, context.Request.Path, exception.GetType().Name);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = ClientClosedRequest;
        }
    }
}
