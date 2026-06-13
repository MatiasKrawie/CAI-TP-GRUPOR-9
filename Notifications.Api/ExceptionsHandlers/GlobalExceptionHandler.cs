using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Api.ExceptionsHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            string errorCode = "NTF-500"; 
            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "Internal Server Error";
            string detail = "No se puede procesar la solicitud.";
            string errorMessage = exception.Message;

            var actualException = exception is AggregateException && exception.InnerException != null
                ? exception.InnerException
                : exception;

            if (actualException is NotificationException ntfEx)
            {
                statusCode = ntfEx.StatusCode;
                errorCode = ntfEx.ErrorCode;
                errorMessage = ntfEx.Message;

                title = ntfEx switch
                {
                    NotFoundException => "Not Found",
                    BusinessRuleException => "Business Rule Violation",
                    ValidationException => "Bad Request",
                    _ => "Domain Error"
                };

                if (statusCode == 422) title = "Unprocessable Entity";

                Log.Warning("Excepción de negocio en Notificaciones: {ErrorCode} - {Message}", errorCode, errorMessage);
            }
            else
            {
                Log.Error(actualException, "Excepción técnica no controlada en Notificaciones.");
            }

            if (!httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
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
                correlationId = correlationId.ToString()
            };

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await httpContext.Response.WriteAsJsonAsync(problemDetails, jsonOptions, cancellationToken);

            return true; 
        }
    }
}