using Core.CrossCuttingConcerns.Exceptions.Handlers;
using Core.CrossCuttingConcerns.Logging;
using Core.CrossCuttingConcerns.Serilog;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Core.CrossCuttingConcerns.Exceptions.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpExceptionHandler _hppExceptionHandler;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly LoggerServiceBase _loggerService;

    public ExceptionMiddleware(RequestDelegate next, IHttpContextAccessor contextAccessor, LoggerServiceBase loggerService)
    {
        _next = next;
        _hppExceptionHandler = new HttpExceptionHandler();
        _contextAccessor = contextAccessor;
        _loggerService = loggerService;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogException(context, ex);
            await HandleExceptionAsyn(context.Response, ex);

        }
    }

    private Task LogException(HttpContext context, Exception ex)
    {
        List<LogParameter> logParameters = new()
        {
            new LogParameter{Type = context.GetType().Name, Value = ex.ToString() }
        };

        LogDetailWithException logDetail = new()
        {
            ExceptionMessage = ex.Message,
            MethodName = _next.Method.Name,
            Parameters = logParameters,
            User = _contextAccessor.HttpContext?.User.Identity?.Name ?? "?"
        };

        _loggerService.Error(System.Text.Json.JsonSerializer.Serialize(logDetail));

        return Task.CompletedTask;
    }

    private Task HandleExceptionAsyn(HttpResponse response, Exception exception)
    {
        response.ContentType = "application/json";
        _hppExceptionHandler.Response = response;
        return _hppExceptionHandler.HandleExceptionAsync(exception);
    }
}
