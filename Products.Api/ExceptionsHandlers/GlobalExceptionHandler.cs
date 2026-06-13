using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Products.Api.ExceptionsHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            string errorCode = "PROD-500"; 
            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "Internal Server Error";
            string detail = "No se puede procesar la solicitud.";
            string errorMessage = exception.Message;

            var exType = exception.GetType().Name;

            if (exType.Contains("NotFoundException") ||
                exType.Contains("BusinessRuleException") ||
                exType.Contains("ValidationException") ||
                exType.Contains("ProductException"))
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

                var errorCodeProp = exception.GetType().GetProperty("ErrorCode", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (errorCodeProp != null)
                {
                    errorCode = errorCodeProp.GetValue(exception)?.ToString() ?? "PROD-400";
                }

                var statusCodeProp = exception.GetType().GetProperty("StatusCode");
                if (statusCodeProp != null)
                {
                    var customStatus = statusCodeProp.GetValue(exception);
                    if (customStatus != null) statusCode = (int)customStatus;
                }

                if (statusCode == 422) title = "Unprocessable Entity";

                Log.Warning("Excepción de negocio capturada en Productos: {ErrorCode} - {Message}", errorCode, errorMessage);
            }
            else
            {
                Log.Error(exception, "Excepción no controlada capturada en Productos.");
            }

            string currentCorrelationId = string.Empty;
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationIdReq))
            {
                currentCorrelationId = correlationIdReq.ToString();
            }
            else if (httpContext.Response.Headers.TryGetValue("X-Correlation-ID", out var correlationIdRes))
            {
                currentCorrelationId = correlationIdRes.ToString();
            }

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