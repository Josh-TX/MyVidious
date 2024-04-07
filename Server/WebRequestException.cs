using Newtonsoft.Json;


public class WebRequestException : Exception
{
    public int StatusCode { get; }

    public WebRequestException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class WebRequestExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public WebRequestExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (WebRequestException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(ex.Message);
        }
    }
}