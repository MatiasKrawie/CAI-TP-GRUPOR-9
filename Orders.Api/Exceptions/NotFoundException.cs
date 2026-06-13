using System;

namespace Orders.Api.Exceptions
{
    // Clase Base Única para las excepciones del dominio de Órdenes
    public class OrderException : Exception
    {
        public string ErrorCode { get; set; }
        public int StatusCode { get; set; }

        public OrderException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    // Excepción para elementos no encontrados (Status 404)
    public class NotFoundException : OrderException
    {
        // Constructor estándar de 2 argumentos
        public NotFoundException(string errorCode, string message)
            : base(errorCode, 404, message) { }

        // Constructor flexible de 3 argumentos para que no rompa tu OrderService actual
        public NotFoundException(string errorCode, int statusCode, string message)
            : base(errorCode, statusCode, message) { }
    }

    // Excepción para violaciones de reglas de negocio (Status 409)
    public class BusinessRuleException : OrderException
    {
        public BusinessRuleException(string errorCode, string message, int statusCode = 409)
            : base(errorCode, statusCode, message) { }
    }

    // Excepción para fallas de validación de datos (Status 400)
    public class ValidationException : OrderException
    {
        public ValidationException(string errorCode, string message)
            : base(errorCode, 400, message) { }
    }
}