using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Products.API.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Products.API.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
             
                Instance = httpContext.Request.Path
            };

            if (exception is ProductException prodEx)
            {
            
                problemDetails.Status = prodEx.StatusCode;

              
                problemDetails.Title = prodEx.StatusCode switch
                {
                    400 => "Bad Request",
                    404 => "Not Found",
                    409 => "Conflict",
                    _ => "Error"
                };

               
                problemDetails.Type = $"https://tools.ietf.org/html/rfc7231#section-6.5.{prodEx.StatusCode switch { 400 => "1", 404 => "4", 409 => "10", _ => "1" }}";

                
                problemDetails.Detail = "El recurso solicitado causó un conflicto o no fue encontrado.";
                problemDetails.Extensions["errorCode"] = prodEx.ErrorCode;
                problemDetails.Extensions["errorMessage"] = prodEx.Message; 

                Log.Warning("Error de catálogo ({ErrorCode}): {Message}", prodEx.ErrorCode, prodEx.Message);
            }
            else
            {
               
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Detail = "Ocurrió un error inesperado en el servidor.";

                problemDetails.Extensions["errorCode"] = "PRD-005";
                problemDetails.Extensions["errorMessage"] = "Error interno al procesar el producto.";

                Log.Error(exception, "Error crítico de infraestructura (PRD-005): {Message}", exception.Message);
            }

           
            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json"; 

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; 
        }
    }
}