namespace TripWise.Application;

public class ExternalServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
