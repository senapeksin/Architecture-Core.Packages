using Core.CrossCuttingConcerns.Exceptions.Handlers;
using Microsoft.AspNetCore.Http;

namespace Core.CrossCuttingConcerns.Exceptions.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpExceptionHandler _hppExceptionHandler;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
        _hppExceptionHandler = new HttpExceptionHandler();
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsyn(context.Response, ex); 
        }
    }


    private Task HandleExceptionAsyn(HttpResponse response, Exception exception)
    {
        response.ContentType = "application/json";
        _hppExceptionHandler.Response = response;
        return _hppExceptionHandler.HandleExceptionAsync(exception);
    }
}
