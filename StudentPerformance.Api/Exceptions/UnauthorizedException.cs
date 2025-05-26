// StudentPerformance.Api.Exceptions/UnauthorizedException.cs
using System;

namespace StudentPerformance.Api.Exceptions // <--- Это пространство имен
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base("Authentication failed.") { }
        public UnauthorizedException(string message) : base(message) { }
        public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
    }
}