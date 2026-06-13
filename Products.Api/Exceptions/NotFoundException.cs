using System;

namespace Products.Api.Exceptions
{
    public class ProductException : Exception
    {
        public string ErrorCode { get; set; } = "PROD-400";
        public int StatusCode { get; set; } = 400;

        public ProductException(string message) : base(message) { }

        public ProductException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public ProductException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : ProductException
    {
        public NotFoundException(string message) : base(message) { StatusCode = 404; }
        public NotFoundException(string errorCode, string message) : base(errorCode, message) { StatusCode = 404; }
        public NotFoundException(string errorCode, int statusCode, string message) : base(errorCode, statusCode, message) { }
    }

    public class BusinessRuleException : ProductException
    {
        public BusinessRuleException(string message) : base(message) { StatusCode = 409; }
        public BusinessRuleException(string errorCode, string message) : base(errorCode, message) { StatusCode = 409; }
        public BusinessRuleException(string errorCode, string message, int statusCode) : base(errorCode, statusCode, message) { }
        public BusinessRuleException(string errorCode, int statusCode, string message) : base(errorCode, statusCode, message) { }
    }

    public class ValidationException : ProductException
    {
        public ValidationException(string message) : base(message) { StatusCode = 400; }
        public ValidationException(string errorCode, string message) : base(errorCode, message) { StatusCode = 400; }
        public ValidationException(string errorCode, int statusCode, string message) : base(errorCode, statusCode, message) { }
    }
}

namespace Products.API.Exceptions
{
    public class NotFoundException : Products.Api.Exceptions.NotFoundException
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string errorCode, string message) : base(errorCode, message) { }
        public NotFoundException(string errorCode, int statusCode, string message) : base(errorCode, statusCode, message) { }
    }

    public class BusinessRuleException : Products.Api.Exceptions.BusinessRuleException
    {
        public BusinessRuleException(string message) : base(message) { }
        public BusinessRuleException(string errorCode, string message) : base(errorCode, message) { }
        public BusinessRuleException(string errorCode, string message, int statusCode) : base(errorCode, statusCode, message) { }
        public BusinessRuleException(string errorCode, int statusCode, string message) : base(errorCode, statusCode, message) { }
    }

    public class ValidationException : Products.Api.Exceptions.ValidationException
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string errorCode, string message) : base(errorCode, message) { }
        public ValidationException(string errorCode, int statusCode, string message) : base(errorCode, statusCode, message) { }
    }
}