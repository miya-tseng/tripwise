namespace TripWise.Application.Dtos;

public class ExchangeRatesDto
{
    public string BaseCurrency { get; set; } = string.Empty;
    public bool Available { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public IReadOnlyList<string> Currencies { get; set; } = [];
    public IReadOnlyDictionary<string, decimal> UnitsPerBaseCurrency { get; set; } = new Dictionary<string, decimal>();
}
