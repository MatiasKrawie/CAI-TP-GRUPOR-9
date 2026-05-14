using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex) return false;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Recurso no encontrado",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Instance = httpContext.Request.Path,
            Detail = ex.Message
        };

        // Aquí agregamos los campos obligatorios del TP
        problemDetails.Extensions.Add("errorCode", ex.ErrorCode);
        problemDetails.Extensions.Add("errorMessage", ex.Message);

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}