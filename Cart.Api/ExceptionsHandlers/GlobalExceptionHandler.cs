using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Cart.Api.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cart.Api.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            if (exception is NotFoundException cartEx)
            {
                problemDetails.Status = cartEx.StatusCode;
                problemDetails.Title = cartEx.StatusCode switch
                {
                    400 => "Bad Request",
                    404 => "Not Found",
                    422 => "Unprocessable Entity",
                    _ => "Error"
                };

                problemDetails.Type = cartEx.StatusCode switch
                {
                    404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
                    _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                };

                problemDetails.Detail = "Ocurrió una falla de validación o recurso no encontrado en el carrito.";
                problemDetails.Extensions["errorCode"] = cartEx.ErrorCode;
                problemDetails.Extensions["errorMessage"] = cartEx.Message;

                Log.Warning("Error de catálogo de carrito ({ErrorCode}): {Message}", cartEx.ErrorCode, cartEx.Message);
            }
            else
            {
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Detail = "Ocurrió un error inesperado en el servidor.";

                problemDetails.Extensions["errorCode"] = "CRT-005";
                problemDetails.Extensions["errorMessage"] = "Error interno al procesar el carrito.";

                Log.Error(exception, "Error crítico en Cart.API (CRT-005): {Message}", exception.Message);
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}