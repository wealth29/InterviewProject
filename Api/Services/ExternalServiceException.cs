namespace Api.Services
{
    public class ExternalServiceException: Exception
    {
        public ExternalServiceException(string message)
            : base(message) { }

        public ExternalServiceException(string message, Exception inner)
            : base(message, inner) { }
    }
}