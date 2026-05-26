using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Users.Api.Exceptions; 
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Users.Api.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            
            if (exception is NotFoundException userEx)
            {
                problemDetails.Status = userEx.StatusCode;

                
                problemDetails.Title = userEx.StatusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    409 => "Conflict",
                    422 => "Unprocessable Entity",
                    _ => "Error"
                };

               
                problemDetails.Type = userEx.StatusCode switch
                {
                    401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
                    403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    409 => "https://tools.ietf.org/html/rfc7231#section-6.5.9",
                    _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                };

               
                problemDetails.Detail = userEx.StatusCode switch
                {
                    400 => "Los datos provistos son inválidos o están incompletos.",
                    401 => "Las credenciales no son válidas.",
                    403 => "El acceso está prohibido.",
                    409 => "Ya existe un recurso con esos datos.",
                    _ => "Ocurrió un error al procesar la solicitud de usuario."
                };

                
                problemDetails.Extensions["errorCode"] = userEx.ErrorCode;
                problemDetails.Extensions["errorMessage"] = userEx.Message;

                
                Log.Warning("Error de catálogo de usuarios ({ErrorCode}): {Message}", userEx.ErrorCode, userEx.Message);
            }
            else
            {
               
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Detail = "Ocurrió un error inesperado en el servidor.";

                problemDetails.Extensions["errorCode"] = "USR-006";
                problemDetails.Extensions["errorMessage"] = "Error interno al procesar el usuario.";

                
                Log.Error(exception, "Error crítico de infraestructura en Users.API (USR-006): {Message}", exception.Message);
            }

            
            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

           
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; 
        }
    }
}