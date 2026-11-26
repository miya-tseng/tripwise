using NSubstitute;
using TripWise.Application;
using TripWise.Application.Currencies;
using TripWise.Application.Places;
using TripWise.Domain;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.UnitTests.Services;

public class ActivityServiceTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private static readonly ExchangeRates Rates = new(
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new Dictionary<string, decimal> { ["JPY"] = 4.8m, ["USD"] = 0.032m });

    private static readonly Place SensoJi = new(
        "senso-ji-id",
        "Senso-ji",
        "2 Chome-3-1 Asakusa, Taito City, Tokyo",
        35.7148,
        139.7967,
        "https://maps.google.com/?cid=1");

    private ITripRepository _repository = null!;
    private IExchangeRateProvider _exchangeRates = null!;
    private IPlaceSearch _placeSearch = null!;
    private ActivityService _sut = null!;
    private Trip _trip = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ITripRepository>();
        _exchangeRates = Substitute.For<IExchangeRateProvider>();
        _exchangeRates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(Rates);
        _placeSearch = Substitute.For<IPlaceSearch>();
        _placeSearch.GetDetailsAsync(SensoJi.PlaceId, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(SensoJi);
        _sut = new ActivityService(_repository, _exchangeRates, new PlaceResolver(_placeSearch));
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
        _repository.GetByIdAsync(_trip.Id, Arg.Any<CancellationToken>()).Returns(_trip);
    }

    private static SaveActivityDto Dto(string name, DateOnly date, string start, string end) => new()
    {
        Name = name,
        Date = date,
        StartTime = TimeOnly.Parse(start),
        EndTime = TimeOnly.Parse(end),
    };

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public async Task AddAsync_WithValidDto_AddsActivityAndSavesTrip()
    {
        var result = await _sut.AddAsync(_trip.Id, Dto("Senso-ji", FirstDay, "10:00", "11:00"));

        await _repository.Received(1).UpdateAsync(_trip, Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(_trip.Activities.Single().Id));
            Assert.That(result.Name, Is.EqualTo("Senso-ji"));
            Assert.That(result.Date, Is.EqualTo(FirstDay));
            Assert.That(result.StartTime, Is.EqualTo(new TimeOnly(10, 0)));
            Assert.That(result.EndTime, Is.EqualTo(new TimeOnly(11, 0)));
        });
    }

    [Test]
    public async Task AddAsync_WithUnknownTrip_ReturnsNullWithoutSaving()
    {
        var result = await _sut.AddAsync(Guid.NewGuid(), Dto("Senso-ji", FirstDay, "10:00", "11:00"));

        Assert.That(result, Is.Null);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddAsync_Overlapping_ThrowsWithoutSaving()
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        Assert.ThrowsAsync<ActivityOverlapException>(() =>
            _sut.AddAsync(_trip.Id, Dto("Ueno Park", FirstDay, "10:30", "11:30")));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithValidDto_UpdatesActivityAndSavesTrip()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        var result = await _sut.UpdateAsync(_trip.Id, activity.Id, Dto("Tokyo Tower", FirstDay, "14:00", "15:00"));

        await _repository.Received(1).UpdateAsync(_trip, Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("Tokyo Tower"));
            Assert.That(result.StartTime, Is.EqualTo(new TimeOnly(14, 0)));
        });
    }

    [Test]
    public async Task UpdateAsync_WithUnknownTrip_ReturnsNull()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), Dto("Senso-ji", FirstDay, "10:00", "11:00"));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_RemovesActivityAndSavesTrip()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        var deleted = await _sut.DeleteAsync(_trip.Id, activity.Id);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(_trip.Activities, Is.Empty);
        });
        await _repository.Received(1).UpdateAsync(_trip, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_WithUnknownTrip_ReturnsFalse()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task GetScheduleAsync_ReturnsEveryTripDayInOrder()
    {
        var schedule = await _sut.GetScheduleAsync(_trip.Id);

        Assert.Multiple(() =>
        {
            Assert.That(schedule!.TripId, Is.EqualTo(_trip.Id));
            Assert.That(
                schedule.Days.Select(day => day.Date),
                Is.EqualTo(new[] { FirstDay, FirstDay.AddDays(1), FirstDay.AddDays(2) }));
            Assert.That(schedule.Days.SelectMany(day => day.Activities), Is.Empty);
        });
    }

    [Test]
    public async Task GetScheduleAsync_GroupsActivitiesByDaySortedByStartTime()
    {
        _trip.AddActivity("Shibuya", FirstDay, Slot("15:00", "16:00"));
        _trip.AddActivity("Tokyo Tower", FirstDay.AddDays(2), Slot("18:00", "19:00"));
        _trip.AddActivity("Tsukiji", FirstDay, Slot("07:00", "08:00"));
        _trip.AddActivity("Senso-ji", FirstDay, Slot("09:00", "10:00"));

        var schedule = await _sut.GetScheduleAsync(_trip.Id);

        Assert.Multiple(() =>
        {
            Assert.That(
                schedule!.Days[0].Activities.Select(activity => activity.Name),
                Is.EqualTo(new[] { "Tsukiji", "Senso-ji", "Shibuya" }));
            Assert.That(schedule.Days[1].Activities, Is.Empty);
            Assert.That(
                schedule.Days[2].Activities.Select(activity => activity.Name),
                Is.EqualTo(new[] { "Tokyo Tower" }));
        });
    }

    [Test]
    public async Task GetScheduleAsync_WithUnknownTrip_ReturnsNull()
    {
        var schedule = await _sut.GetScheduleAsync(Guid.NewGuid());

        Assert.That(schedule, Is.Null);
    }

    [Test]
    public async Task AddAsync_WithCost_ReturnsCost()
    {
        var dto = Dto("Shibuya Sky", FirstDay, "17:00", "18:00");
        dto.Cost = 2200.5m;

        var result = await _sut.AddAsync(_trip.Id, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Cost, Is.EqualTo(2200.5m));
            Assert.That(_trip.Activities.Single().Cost, Is.EqualTo(new Money(2200.5m)));
        });
    }

    [Test]
    public async Task AddAsync_WithoutCost_UsesZero()
    {
        var result = await _sut.AddAsync(_trip.Id, Dto("Senso-ji", FirstDay, "10:00", "11:00"));

        Assert.That(result!.Cost, Is.Zero);
    }

    [Test]
    public async Task AddAsync_WithNegativeCost_ThrowsWithoutSaving()
    {
        var dto = Dto("Senso-ji", FirstDay, "10:00", "11:00");
        dto.Cost = -1m;

        Assert.ThrowsAsync<DomainException>(() => _sut.AddAsync(_trip.Id, dto));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
        Assert.That(_trip.Activities, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_WithCost_UpdatesCost()
    {
        var activity = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));
        var dto = Dto("Shibuya Sky", FirstDay, "17:00", "18:00");
        dto.Cost = 2500m;

        var result = await _sut.UpdateAsync(_trip.Id, activity.Id, dto);

        Assert.That(result!.Cost, Is.EqualTo(2500m));
    }

    [Test]
    public async Task GetScheduleAsync_ReturnsDailyAndTripTotals()
    {
        _trip.AddActivity("Tsukiji", FirstDay, Slot("07:00", "08:00"), new Money(1500.5m));
        _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));
        _trip.AddActivity("teamLab", FirstDay.AddDays(2), Slot("13:00", "15:00"), new Money(3800.25m));

        var schedule = await _sut.GetScheduleAsync(_trip.Id);

        Assert.Multiple(() =>
        {
            Assert.That(schedule!.Days.Select(day => day.TotalInTwd), Is.EqualTo(new decimal?[] { 3700.5m, 0m, 3800.25m }));
            Assert.That(schedule.TotalInTwd, Is.EqualTo(7500.75m));
            Assert.That(schedule.Days[0].Activities.Select(activity => activity.Cost), Is.EqualTo(new[] { 1500.5m, 2200m }));
        });
    }

    [Test]
    public async Task AddAsync_WithForeignCurrency_KeepsOriginalAndConvertsToTwd()
    {
        var dto = Dto("Shibuya Sky", FirstDay, "17:00", "18:00");
        dto.Cost = 2200m;
        dto.Currency = "jpy";

        var result = await _sut.AddAsync(_trip.Id, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Cost, Is.EqualTo(2200m));
            Assert.That(result.Currency, Is.EqualTo("JPY"));
            Assert.That(result.CostInTwd, Is.EqualTo(458.33m));
            Assert.That(_trip.Activities.Single().Cost, Is.EqualTo(new Money(2200m, "JPY")));
        });
    }

    [Test]
    public async Task AddAsync_WithoutCurrency_UsesTwd()
    {
        var dto = Dto("Senso-ji", FirstDay, "10:00", "11:00");
        dto.Cost = 150m;

        var result = await _sut.AddAsync(_trip.Id, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Currency, Is.EqualTo("TWD"));
            Assert.That(result.CostInTwd, Is.EqualTo(150m));
        });
    }

    [Test]
    public async Task AddAsync_WithUnsupportedCurrency_ThrowsWithoutSaving()
    {
        var dto = Dto("Senso-ji", FirstDay, "10:00", "11:00");
        dto.Cost = 10m;
        dto.Currency = "XYZ";

        var ex = Assert.ThrowsAsync<DomainException>(() => _sut.AddAsync(_trip.Id, dto));

        Assert.That(ex!.Message, Is.EqualTo("Currency XYZ is not supported."));
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddAsync_WhenRatesAreUnavailable_AcceptsAnyValidCurrency()
    {
        _exchangeRates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((ExchangeRates?)null);
        var dto = Dto("Senso-ji", FirstDay, "10:00", "11:00");
        dto.Cost = 10m;
        dto.Currency = "XYZ";

        var result = await _sut.AddAsync(_trip.Id, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Currency, Is.EqualTo("XYZ"));
            Assert.That(result.CostInTwd, Is.Null);
        });
    }

    [Test]
    public async Task GetScheduleAsync_WithSeveralCurrencies_ConvertsTotalsToTwd()
    {
        _trip.AddActivity("Tsukiji", FirstDay, Slot("07:00", "08:00"), new Money(3000m, "JPY"));
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), new Money(150m));
        _trip.AddActivity("Airport bus", FirstDay.AddDays(1), Slot("13:00", "15:00"), new Money(10m, "USD"));

        var schedule = await _sut.GetScheduleAsync(_trip.Id);

        Assert.Multiple(() =>
        {
            Assert.That(schedule!.RatesAvailable, Is.True);
            Assert.That(schedule.RatesUpdatedAt, Is.EqualTo(Rates.UpdatedAt));
            Assert.That(schedule.Days[0].Activities.Select(activity => activity.CostInTwd), Is.EqualTo(new decimal?[] { 625m, 150m }));
            Assert.That(schedule.Days.Select(day => day.TotalInTwd), Is.EqualTo(new decimal?[] { 775m, 312.5m, 0m }));
            Assert.That(schedule.TotalInTwd, Is.EqualTo(1087.5m));
            Assert.That(schedule.Totals.Select(total => (total.Amount, total.Currency)), Is.EqualTo(new[] { (150m, "TWD"), (3000m, "JPY"), (10m, "USD") }));
            Assert.That(schedule.Days[0].Totals.Select(total => total.Currency), Is.EqualTo(new[] { "TWD", "JPY" }));
        });
    }

    [Test]
    public async Task GetScheduleAsync_WhenRatesAreUnavailable_KeepsOriginalTotals()
    {
        _exchangeRates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((ExchangeRates?)null);
        _trip.AddActivity("Tsukiji", FirstDay, Slot("07:00", "08:00"), new Money(3000m, "JPY"));

        var schedule = await _sut.GetScheduleAsync(_trip.Id);

        Assert.Multiple(() =>
        {
            Assert.That(schedule!.RatesAvailable, Is.False);
            Assert.That(schedule.RatesUpdatedAt, Is.Null);
            Assert.That(schedule.TotalInTwd, Is.Null);
            Assert.That(schedule.Days[0].TotalInTwd, Is.Null);
            Assert.That(schedule.Days[0].Activities[0].CostInTwd, Is.Null);
            Assert.That(schedule.Totals.Single().Amount, Is.EqualTo(3000m));
        });
    }

    [Test]
    public async Task AddAsync_WithPlaceId_LoadsPlaceDetailsAndDefaultsName()
    {
        var dto = Dto("", FirstDay, "10:00", "11:00");
        dto.PlaceId = SensoJi.PlaceId;
        dto.PlaceSessionToken = "session-1";

        var result = await _sut.AddAsync(_trip.Id, dto);

        await _placeSearch.Received(1).GetDetailsAsync(SensoJi.PlaceId, "session-1", Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("Senso-ji"));
            Assert.That(result.Place!.PlaceId, Is.EqualTo(SensoJi.PlaceId));
            Assert.That(result.Place.Address, Is.EqualTo(SensoJi.Address));
            Assert.That(result.Place.Latitude, Is.EqualTo(SensoJi.Latitude));
            Assert.That(result.Place.GoogleMapsUri, Is.EqualTo(SensoJi.GoogleMapsUri));
            Assert.That(_trip.Activities.Single().Place, Is.EqualTo(SensoJi));
        });
    }

    [Test]
    public async Task AddAsync_WithoutPlaceId_DoesNotSearch()
    {
        var result = await _sut.AddAsync(_trip.Id, Dto("Free time", FirstDay, "10:00", "11:00"));

        Assert.That(result!.Place, Is.Null);
        await _placeSearch.DidNotReceiveWithAnyArgs().GetDetailsAsync(default!, default, default);
    }

    [Test]
    public async Task AddAsync_WithUnknownPlace_ThrowsWithoutSaving()
    {
        var dto = Dto("Somewhere", FirstDay, "10:00", "11:00");
        dto.PlaceId = "unknown";

        var ex = Assert.ThrowsAsync<DomainException>(() => _sut.AddAsync(_trip.Id, dto));

        Assert.That(ex!.Message, Is.EqualTo("Place not found."));
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddAsync_WhenPlaceSearchIsUnavailable_ThrowsWithoutSaving()
    {
        _placeSearch.GetDetailsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Place?>(_ => throw new ExternalServiceUnavailableException("down"));
        var dto = Dto("", FirstDay, "10:00", "11:00");
        dto.PlaceId = SensoJi.PlaceId;

        Assert.ThrowsAsync<ExternalServiceUnavailableException>(() => _sut.AddAsync(_trip.Id, dto));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
        Assert.That(_trip.Activities, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_WithSamePlace_ReusesStoredPlaceWithoutSearching()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), place: SensoJi);
        var dto = Dto("Senso-ji", FirstDay, "13:00", "14:00");
        dto.PlaceId = SensoJi.PlaceId;

        await _sut.UpdateAsync(_trip.Id, activity.Id, dto);

        await _placeSearch.DidNotReceiveWithAnyArgs().GetDetailsAsync(default!, default, default);
        Assert.That(activity.Place, Is.EqualTo(SensoJi));
    }

    [Test]
    public async Task UpdateAsync_WithoutPlaceId_RemovesPlace()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        await _sut.UpdateAsync(_trip.Id, activity.Id, Dto("Free time", FirstDay, "10:00", "11:00"));

        Assert.That(activity.Place, Is.Null);
    }

    [Test]
    public async Task MoveAsync_MovesTheActivityAndSaves()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"), new Money(3000m, "JPY"));

        var result = await _sut.MoveAsync(
            _trip.Id,
            activity.Id,
            new MoveActivityDto { Date = FirstDay.AddDays(1), StartTime = new TimeOnly(14, 0) });

        await _repository.Received(1).UpdateAsync(_trip, Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result!.Date, Is.EqualTo(FirstDay.AddDays(1)));
            Assert.That(result.StartTime, Is.EqualTo(new TimeOnly(14, 0)));
            Assert.That(result.EndTime, Is.EqualTo(new TimeOnly(15, 30)));
            Assert.That(result.CostInTwd, Is.EqualTo(625m));
        });
    }

    [Test]
    public async Task MoveAsync_WithUnknownTrip_ReturnsNull()
    {
        var result = await _sut.MoveAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new MoveActivityDto { Date = FirstDay, StartTime = new TimeOnly(10, 0) });

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task MoveAsync_Overlapping_ThrowsWithoutSaving()
    {
        _trip.AddActivity("Ueno Park", FirstDay, Slot("14:00", "15:00"));
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"));

        Assert.ThrowsAsync<ActivityOverlapException>(() => _sut.MoveAsync(
            _trip.Id,
            activity.Id,
            new MoveActivityDto { Date = FirstDay, StartTime = new TimeOnly(13, 30) }));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddAsync_WithDetails_ReturnsThem()
    {
        var dto = Dto("Shibuya Sky", FirstDay, "17:00", "18:00");
        dto.Category = ActivityCategory.Sightseeing;
        dto.BookingStatus = BookingStatus.ToBook;
        dto.Notes = " Sunset ";
        dto.ConfirmationCode = "SKY-1";
        dto.Link = "https://example.com/sky";

        var result = await _sut.AddAsync(_trip.Id, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Category, Is.EqualTo(ActivityCategory.Sightseeing));
            Assert.That(result.BookingStatus, Is.EqualTo(BookingStatus.ToBook));
            Assert.That(result.Notes, Is.EqualTo("Sunset"));
            Assert.That(result.ConfirmationCode, Is.EqualTo("SKY-1"));
            Assert.That(result.Link, Is.EqualTo("https://example.com/sky"));
        });
    }

    [Test]
    public async Task AddAsync_WithoutDetails_UsesDefaults()
    {
        var result = await _sut.AddAsync(_trip.Id, Dto("Free time", FirstDay, "15:00", "16:00"));

        Assert.Multiple(() =>
        {
            Assert.That(result!.Category, Is.EqualTo(ActivityCategory.Other));
            Assert.That(result.BookingStatus, Is.EqualTo(BookingStatus.NotNeeded));
            Assert.That(result.Link, Is.Null);
        });
    }
}
