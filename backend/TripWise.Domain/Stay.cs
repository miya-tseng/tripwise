namespace TripWise.Domain;

public class Stay
{
    public const int MaxNameLength = 150;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Place? Place { get; private set; }
    public DateOnly CheckIn { get; private set; }
    public DateOnly CheckOut { get; private set; }
    public Money Cost { get; private set; } = Money.Zero;
    public BookingStatus BookingStatus { get; private set; }
    public string ConfirmationCode { get; private set; } = string.Empty;

    private Stay() { }

    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

    public bool CoversNight(DateOnly night) => CheckIn <= night && night < CheckOut;

    internal bool OverlapsWith(DateOnly checkIn, DateOnly checkOut) => CheckIn < checkOut && checkIn < CheckOut;

    public Money CostForNight(DateOnly night)
    {
        if (!CoversNight(night))
        {
            return Money.ZeroIn(Cost.Currency);
        }

        var perNight = decimal.Round(Cost.Amount / Nights, Money.DecimalPlaces, MidpointRounding.ToZero);
        var isLastNight = night.AddDays(1) == CheckOut;
        var amount = isLastNight ? Cost.Amount - perNight * (Nights - 1) : perNight;

        return new Money(amount, Cost.Currency);
    }

    internal static Stay Create(StayDetails details) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = details.Name,
        Place = details.Place,
        CheckIn = details.CheckIn,
        CheckOut = details.CheckOut,
        Cost = details.Cost,
        BookingStatus = details.BookingStatus,
        ConfirmationCode = details.ConfirmationCode,
    };

    internal void Update(StayDetails details)
    {
        Name = details.Name;
        Place = details.Place;
        CheckIn = details.CheckIn;
        CheckOut = details.CheckOut;
        Cost = details.Cost;
        BookingStatus = details.BookingStatus;
        ConfirmationCode = details.ConfirmationCode;
    }
}
