using System;

namespace Users.Api.Exceptions
{
    public class UserException : Exception
    {
        public string ErrorCode { get; set; }
        public int StatusCode { get; set; }

        public UserException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : UserException
    {
        public NotFoundException(string errorCode, string message)
            : base(errorCode, 404, message) { }

        public NotFoundException(string errorCode, int statusCode, string message)
            : base(errorCode, statusCode, message) { }
    }

    public class BusinessRuleException : UserException
    {
        public BusinessRuleException(string errorCode, string message, int statusCode = 409)
            : base(errorCode, statusCode, message) { }
    }

    public class ValidationException : UserException
    {
        public ValidationException(string errorCode, string message)
            : base(errorCode, 400, message) { }
    }
}