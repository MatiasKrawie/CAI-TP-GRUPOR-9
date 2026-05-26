using System;

namespace Users.Api.Exceptions
{
    public class NotFoundException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        
        public NotFoundException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}