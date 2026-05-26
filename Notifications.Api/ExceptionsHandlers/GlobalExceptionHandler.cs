using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Notifications.Api.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Api.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            if (exception is NotFoundException ntfEx)
            {
                problemDetails.Status = ntfEx.StatusCode;
                problemDetails.Title = ntfEx.StatusCode switch
                {
                    400 => "Bad Request",
                    404 => "Not Found",
                    _ => "Error"
                };

                problemDetails.Type = ntfEx.StatusCode switch
                {
                    404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                };

                problemDetails.Detail = "Ocurrió una excepción de negocio controlada en el servicio de notificaciones.";
                problemDetails.Extensions["errorCode"] = ntfEx.ErrorCode;
                problemDetails.Extensions["errorMessage"] = ntfEx.Message;

                Log.Warning("Error de catálogo de notificaciones ({ErrorCode}): {Message}", ntfEx.ErrorCode, ntfEx.Message);
            }
            else
            {
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Detail = "Ocurrió un error inesperado en el servidor.";

                problemDetails.Extensions["errorCode"] = "NTF-004";
                problemDetails.Extensions["errorMessage"] = "Error interno al procesar la notificación.";

                Log.Error(exception, "Error crítico de infraestructura en Notifications.API (NTF-004): {Message}", exception.Message);
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}