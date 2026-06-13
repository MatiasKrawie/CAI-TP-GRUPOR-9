using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cart.Api.ExceptionsHandlers
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
            string errorCode = "CRT-500";
            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "Internal Server Error";
            string message = exception.Message;

            var exType = exception.GetType().Name;

            // Manejo elástico de excepciones de dominio del Carrito
            if (exType.Contains("NotFoundException") || exType.Contains("BusinessRuleException") || exType.Contains("ValidationException") || exType.Contains("CartException"))
            {
                statusCode = exType switch
                {
                    "NotFoundException" => StatusCodes.Status404NotFound,
                    "BusinessRuleException" => StatusCodes.Status409Conflict,
                    "ValidationException" => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status400BadRequest
                };

                title = exType switch
                {
                    "NotFoundException" => "Not Found",
                    "BusinessRuleException" => "Business Rule Violation",
                    "ValidationException" => "Bad Request",
                    _ => "Domain Error"
                };

                var errorCodeProp = exception.GetType().GetProperty("ErrorCode");
                if (errorCodeProp != null)
                {
                    errorCode = errorCodeProp.GetValue(exception)?.ToString() ?? "CRT-400";
                }

                Log.Warning("Excepción de negocio capturada en Carrito: {ErrorCode} - {Message}", errorCode, message);
            }
            else
            {
                Log.Error(exception, "Excepción no controlada capturada en Carrito.");
            }

            string currentCorrelationId = httpContext.Response.Headers["X-Correlation-Id"].ToString();

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = message,
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        { "errorCode", errorCode },
                        { "correlationId", currentCorrelationId }
                    }
                }
            });
        }
    }
}