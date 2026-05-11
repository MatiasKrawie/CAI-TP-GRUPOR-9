using Serilog;
using Serilog.Events;

namespace WebApplication3.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder app)
        {
            return app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    
                    if (ex != null || httpContext.Response.StatusCode >= 500)
                        return LogEventLevel.Error;

                   
                    if (httpContext.Request.Path.StartsWithSegments("/health"))
                        return LogEventLevel.Verbose;

                    
                    return LogEventLevel.Information;
                };
            });
        }
    }
}