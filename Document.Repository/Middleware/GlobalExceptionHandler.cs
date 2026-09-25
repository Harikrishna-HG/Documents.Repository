using Microsoft.AspNetCore.Diagnostics;

namespace Document.Repository.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}, Method: {Method}, User: {User}",
                    context.Request.Path,
                    context.Request.Method,
                    context.User?.Identity?.Name ?? "Anonymous");

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = _env.IsDevelopment() 
                    ? exception.Message 
                    : "An error occurred. Please try again later.",
                Details = _env.IsDevelopment() 
                    ? exception.StackTrace 
                    : null
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
