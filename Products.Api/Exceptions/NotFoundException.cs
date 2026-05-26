using System;

namespace Products.API.Exceptions
{
    public class ProductException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        public ProductException(string errorCode, int statusCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}