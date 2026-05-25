using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

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

        public static WebApplication UseAppPipeline(this WebApplication app)
        {
            
            app.UseCustomRequestLogging();

            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

          
            app.UseHttpsRedirection();

           
            app.MapProductEndpoints();

            // Endpoint JSON con estado detallado
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            // Dashboard web
            app.MapHealthChecksUI(setup => setup.UIPath = "/health-ui");

            return app; 
        }
    }
}