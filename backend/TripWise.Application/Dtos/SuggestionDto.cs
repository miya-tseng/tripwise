namespace TripWise.Application.Dtos;

public class SuggestionDto
{
    public PlaceDto Place { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public int SuggestedMinutes { get; set; }
}
