namespace StudentPerformance.Api.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
        public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
    }
}