using System.ComponentModel.DataAnnotations;
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }
    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);
            
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var statusCode = MapStatusCode(ex);
        
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex.Message
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }
    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            //  Ошибки клиента
            ArgumentException => StatusCodes.Status400BadRequest,
            ValidationException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            EventValidationException => StatusCodes.Status400BadRequest,
            // Ресурс не найден
             KeyNotFoundException => StatusCodes.Status404NotFound,
             BookingNotFoundException => StatusCodes.Status404NotFound, 
             EventNotFoundException => StatusCodes.Status404NotFound,
             _ => StatusCodes.Status500InternalServerError
        };
}
