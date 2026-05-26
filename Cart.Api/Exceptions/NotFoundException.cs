using System;

namespace Cart.Api.Exceptions
{
    public class NotFoundException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        // El constructor recibe el código de negocio (CRT-00X), el código HTTP y el mensaje descriptivo
        public NotFoundException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}