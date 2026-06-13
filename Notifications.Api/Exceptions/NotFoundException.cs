using System;

namespace Notifications.Api.Exceptions
{
    public class NotificationException : Exception
    {
        public string ErrorCode { get; set; }
        public int StatusCode { get; set; }

        public NotificationException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : NotificationException
    {
        public NotFoundException(string errorCode, string message)
            : base(errorCode, 404, message) { }

        public NotFoundException(string errorCode, int statusCode, string message)
            : base(errorCode, statusCode, message) { }
    }

    public class BusinessRuleException : NotificationException
    {
        public BusinessRuleException(string errorCode, string message, int statusCode = 409)
            : base(errorCode, statusCode, message) { }
    }

    public class ValidationException : NotificationException
    {
        public ValidationException(string errorCode, string message)
            : base(errorCode, 400, message) { }
    }
}