using System;

namespace Cart.Api.Exceptions
{
    public class DomainException : Exception
    {
        public string ErrorCode { get; set; }
        public int StatusCode { get; set; }

        public DomainException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : DomainException
    {
        public NotFoundException(string errorCode, string message)
            : base(errorCode, 404, message) { }

        public NotFoundException(string errorCode, int statusCode, string message)
            : base(errorCode, statusCode, message) { }
    }

    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string errorCode, string message, int statusCode = 409)
            : base(errorCode, statusCode, message) { }
    }

    public class ValidationException : DomainException
    {
        public ValidationException(string errorCode, string message)
            : base(errorCode, 400, message) { }
    }
}