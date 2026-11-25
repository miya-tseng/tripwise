using NSubstitute;
using TripWise.Domain;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.UnitTests.Services;

public class ActivityServiceTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private ITripRepository _repository = null!;
    private ActivityService _sut = null!;
    private Trip _trip = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ITripRepository>();
        _sut = new ActivityService(_repository);
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
            Assert.That(schedule!.Days.Select(day => day.Total), Is.EqualTo(new[] { 3700.5m, 0m, 3800.25m }));
            Assert.That(schedule.Total, Is.EqualTo(7500.75m));
            Assert.That(schedule.Days[0].Activities.Select(activity => activity.Cost), Is.EqualTo(new[] { 1500.5m, 2200m }));
        });
    }
}
