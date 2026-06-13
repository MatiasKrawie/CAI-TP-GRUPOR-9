using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Exceptions; 
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Api.ExceptionsHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            string errorCode = "NTF-500";
            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "Internal Server Error";
            string message = exception.Message;

            var actualException = exception is AggregateException && exception.InnerException != null
                ? exception.InnerException
                : exception;

            if (actualException is NotificationException ntfEx)
            {
                statusCode = ntfEx.StatusCode;
                errorCode = ntfEx.ErrorCode;
                message = ntfEx.Message;

                title = ntfEx switch
                {
                    NotFoundException => "Not Found",
                    BusinessRuleException => "Business Rule Violation",
                    ValidationException => "Bad Request",
                    _ => "Domain Error"
                };

                Log.Warning("Excepción de negocio en Notificaciones: {ErrorCode} - {Message}", errorCode, message);
            }
            else
            {
                Log.Error(actualException, "Excepción técnica no controlada en Notificaciones.");
            }

            if (!httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = actualException,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = message,
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        { "errorCode", errorCode },
                        { "correlationId", correlationId.ToString() }
                    }
                }
            });
        }
    }
}