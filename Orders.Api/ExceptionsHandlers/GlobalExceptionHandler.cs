using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Api.ExceptionsHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            string errorCode = "ORD-007"; 
            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "Internal Server Error";
            string detail = "No se puede procesar la solicitud.";
            string errorMessage = exception.Message;

            var exType = exception.GetType().Name;

            if (exType.Contains("NotFoundException") || exType.Contains("BusinessRuleException") || exType.Contains("ValidationException") || exType.Contains("OrderException"))
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

                var statusCodeProp = exception.GetType().GetProperty("StatusCode");
                if (statusCodeProp != null)
                {
                    var customStatus = statusCodeProp.GetValue(exception);
                    if (customStatus != null) statusCode = (int)customStatus;
                }

                if (statusCode == 422) title = "Unprocessable Entity";

                var errorCodeProp = exception.GetType().GetProperty("ErrorCode");
                if (errorCodeProp != null)
                {
                    errorCode = errorCodeProp.GetValue(exception)?.ToString() ?? "ORD-400";
                }

                Log.Warning("Excepción de negocio capturada en Órdenes: {ErrorCode} - {Message}", errorCode, errorMessage);
            }
            else
            {
                Log.Error(exception, "Excepción no controlada capturada en Órdenes.");
            }

            string currentCorrelationId = httpContext.Response.Headers["X-Correlation-Id"].ToString();
            if (string.IsNullOrEmpty(currentCorrelationId))
            {
                currentCorrelationId = Guid.NewGuid().ToString();
            }

            var problemDetails = new
            {
                type = statusCode switch
                {
                    404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
                    400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                },
                title = title,
                status = statusCode,
                detail = detail,
                instance = httpContext.Request.Path.Value,
                errorCode = errorCode,
                errorMessage = errorMessage,
                correlationId = currentCorrelationId 
            };

           
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await httpContext.Response.WriteAsJsonAsync(problemDetails, jsonOptions, cancellationToken);

            return true; 
        }
    }
}