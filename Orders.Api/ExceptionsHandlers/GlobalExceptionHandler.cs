using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Orders.Api.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Api.ExceptionsHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            if (exception is NotFoundException ordEx)
            {
                problemDetails.Status = ordEx.StatusCode;
                problemDetails.Title = ordEx.StatusCode switch
                {
                    400 => "Bad Request",
                    404 => "Not Found",
                    409 => "Conflict",
                    422 => "Unprocessable Entity",
                    _ => "Error"
                };

                problemDetails.Type = $"https://tools.ietf.org/html/rfc7231#section-6.5.{ordEx.StatusCode switch { 400 => "1", 404 => "4", 409 => "10", 422 => "11", _ => "1" }}";
                problemDetails.Detail = "La operación con la orden causó un error de negocio.";
                problemDetails.Extensions["errorCode"] = ordEx.ErrorCode;
                problemDetails.Extensions["errorMessage"] = ordEx.Message;

                Log.Warning("Error de catálogo de órdenes ({ErrorCode}): {Message}", ordEx.ErrorCode, ordEx.Message);
            }
            else
            {
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Detail = "Ocurrió un error inesperado en el servidor.";
                problemDetails.Extensions["errorCode"] = "ORD-007";
                problemDetails.Extensions["errorMessage"] = "Error interno al procesar la orden.";

                Log.Error(exception, "Error crítico no controlado (ORD-007): {Message}", exception.Message);
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}