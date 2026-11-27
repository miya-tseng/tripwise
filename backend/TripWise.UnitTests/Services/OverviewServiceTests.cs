using NSubstitute;
using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Application.Services;
using TripWise.Domain;

namespace TripWise.UnitTests.Services;

public class OverviewServiceTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private static readonly ExchangeRates Rates = new(
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new Dictionary<string, decimal> { ["TWD"] = 1m, ["JPY"] = 5m });

    private ITripRepository _repository = null!;
    private IExchangeRateProvider _exchangeRates = null!;
    private OverviewService _sut = null!;
    private Trip _trip = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ITripRepository>();
        _exchangeRates = Substitute.For<IExchangeRateProvider>();
        _exchangeRates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(Rates);
        _sut = new OverviewService(_repository, _exchangeRates);
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(1));
        _repository.GetByIdAsync(_trip.Id, Arg.Any<CancellationToken>()).Returns(_trip);
    }

    private Activity Add(string name, int day, int hour, Money? cost = null, ActivityCategory category = ActivityCategory.Other, BookingStatus booking = BookingStatus.NotNeeded) =>
        _trip.AddActivity(
            name,
            FirstDay.AddDays(day),
            new TimeSlot(new TimeOnly(hour, 0), new TimeOnly(hour + 1, 0)),
            cost,
            details: new ActivityDetails(category, booking));

    [Test]
    public void GetAsync_UnknownTrip_Throws()
    {
        Assert.ThrowsAsync<TripNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetAsync_ConvertsSpendToTwd()
    {
        Add("Sushi", 0, 7, new Money(3000, "JPY"), ActivityCategory.Food);
        Add("Senso-ji", 0, 9, new Money(100, "TWD"), ActivityCategory.Sightseeing);
        Add("Walk", 1, 9);
        _trip.AddStay(new StayDetails("Hotel", FirstDay, FirstDay.AddDays(2), cost: new Money(9000, "TWD")));

        var overview = (await _sut.GetAsync(_trip.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(overview.Name, Is.EqualTo("Tokyo"));
            Assert.That(overview.RatesAvailable, Is.True);
            Assert.That(overview.RatesUpdatedAt, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(overview.ActivitiesInTwd, Is.EqualTo(700m));
            Assert.That(overview.StaysInTwd, Is.EqualTo(9000m));
            Assert.That(overview.TotalInTwd, Is.EqualTo(9700m));
            Assert.That(overview.AveragePerDayInTwd, Is.EqualTo(4850m));
            Assert.That(
                overview.SpendByCategory.Select(spend => (spend.Category, spend.ActivityCount, spend.AmountInTwd)),
                Is.EqualTo(new[]
                {
                    (ActivityCategory.Food, 1, (decimal?)600m),
                    (ActivityCategory.Sightseeing, 1, (decimal?)100m),
                    (ActivityCategory.Other, 1, (decimal?)0m),
                }));
            Assert.That(
                overview.SpendByDay.Select(day => (day.ActivitiesInTwd, day.StayInTwd, day.TotalInTwd)),
                Is.EqualTo(new[]
                {
                    ((decimal?)700m, (decimal?)4500m, (decimal?)5200m),
                    ((decimal?)0m, (decimal?)4500m, (decimal?)4500m),
                }));
            Assert.That(
                overview.SpendByCurrency.Select(spend => (spend.Currency, spend.Amount, spend.AmountInTwd)),
                Is.EqualTo(new[] { ("TWD", 9100m, (decimal?)9100m), ("JPY", 3000m, (decimal?)600m) }));
        });
    }

    [Test]
    public async Task GetAsync_ListsTheMostExpensiveItemsFirst()
    {
        for (var hour = 8; hour < 14; hour++)
        {
            Add($"Stop {hour}", 0, hour, new Money(hour * 100, "TWD"));
        }
        _trip.AddStay(new StayDetails("Hotel", FirstDay, FirstDay.AddDays(1), cost: new Money(5250, "JPY")));

        var top = (await _sut.GetAsync(_trip.Id))!.TopExpenses;

        Assert.Multiple(() =>
        {
            Assert.That(top.Select(expense => expense.Name), Is.EqualTo(new[] { "Stop 13", "Stop 12", "Stop 11", "Hotel", "Stop 10" }));
            Assert.That(top[3].Kind, Is.EqualTo(ExpenseKind.Stay));
            Assert.That(top[3].Cost.Currency, Is.EqualTo("JPY"));
            Assert.That(top[3].CostInTwd, Is.EqualTo(1050m));
        });
    }

    [Test]
    public async Task GetAsync_ListsWhatNeedsAttentionInOrder()
    {
        var teamLab = Add("teamLab", 0, 10, booking: BookingStatus.ToBook);
        var inn = _trip.AddStay(new StayDetails("Inn", FirstDay, FirstDay.AddDays(1), bookingStatus: BookingStatus.ToBook));

        var overview = (await _sut.GetAsync(_trip.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(
                overview.Attention.Select(item => (item.Kind, item.Date, item.ItemId)),
                Is.EqualTo(new[]
                {
                    (AttentionKind.ActivityToBook, FirstDay, (Guid?)teamLab.Id),
                    (AttentionKind.StayToBook, FirstDay, (Guid?)inn.Id),
                    (AttentionKind.NightWithoutStay, FirstDay.AddDays(1), (Guid?)null),
                    (AttentionKind.EmptyDay, FirstDay.AddDays(1), (Guid?)null),
                }));
            Assert.That(overview.Attention[0].Name, Is.EqualTo("teamLab"));
            Assert.That(overview.Booking.ToBook, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetAsync_WithoutRates_LeavesTwdAmountsEmpty()
    {
        _exchangeRates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((ExchangeRates?)null);
        Add("Sushi", 0, 7, new Money(3000, "JPY"), ActivityCategory.Food);

        var overview = (await _sut.GetAsync(_trip.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(overview.RatesAvailable, Is.False);
            Assert.That(overview.RatesUpdatedAt, Is.Null);
            Assert.That(overview.TotalInTwd, Is.Null);
            Assert.That(overview.AveragePerDayInTwd, Is.Null);
            Assert.That(overview.SpendByCategory[0].AmountInTwd, Is.Null);
            Assert.That(overview.SpendByDay[0].TotalInTwd, Is.Null);
            Assert.That(overview.SpendByCurrency.Single().Amount, Is.EqualTo(3000m));
            Assert.That(overview.TopExpenses.Single().CostInTwd, Is.Null);
        });
    }

    [Test]
    public async Task GetAsync_CountsTheChecklistAndFlagsOpenItems()
    {
        var passport = _trip.AddChecklistItem("Passport", ChecklistSection.ToPack);
        _trip.AddChecklistItem("Charger", ChecklistSection.ToPack);
        _trip.AddChecklistItem("Adapter", ChecklistSection.ToPack);
        _trip.SetChecklistItemDone(passport.Id, true);

        var overview = (await _sut.GetAsync(_trip.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(overview.ChecklistDone, Is.EqualTo(1));
            Assert.That(overview.ChecklistTotal, Is.EqualTo(3));
            Assert.That(overview.Attention[0].Kind, Is.EqualTo(AttentionKind.ChecklistOpen));
            Assert.That(overview.Attention[0].Name, Is.EqualTo("2 checklist items left"));
        });
    }
}
