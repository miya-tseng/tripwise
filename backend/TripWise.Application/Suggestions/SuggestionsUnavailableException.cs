namespace TripWise.Application.Suggestions;

public class SuggestionsUnavailableException(Exception? innerException = null)
    : ExternalServiceUnavailableException("Suggestions are unavailable right now. Please try again later.", innerException);
