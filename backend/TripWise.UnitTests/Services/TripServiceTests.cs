using NSubstitute;
using TripWise.Domain;
using TripWise.Application.Dtos;
using TripWise.Application.Places;
using TripWise.Application.Services;

namespace TripWise.UnitTests.Services;

public class TripServiceTests
{
    private static readonly DateOnly Start = new(2021, 05, 01);
    private static readonly Place Tokyo = new("city-tokyo", "Tokyo", "Japan", 35.68, 139.65, "https://maps.google.com/?cid=1");

    private ITripRepository _repository = null!;
    private IPlaceSearch _placeSearch = null!;
    private TripService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ITripRepository>();
        _placeSearch = Substitute.For<IPlaceSearch>();
        _placeSearch.GetDetailsAsync(Tokyo.PlaceId, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Tokyo);
        _sut = new TripService(_repository, new PlaceResolver(_placeSearch));
    }

    [Test]
    public async Task CreateAsync_WithValidDto_AddsTripAndReturnsDto()
    {
        var dto = new CreateTripDto
        {
            Name = "Tokyo",
            StartDate = Start,
            EndDate = Start.AddDays(2),
        };

        var result = await _sut.CreateAsync(dto);

        await _repository.Received(1).AddAsync(Arg.Is<Trip>(trip => trip.Id == result.Id), Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Name, Is.EqualTo("Tokyo"));
            Assert.That(result.StartDate, Is.EqualTo(Start));
            Assert.That(result.EndDate, Is.EqualTo(Start.AddDays(2)));
            Assert.That(result.Days, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task CreateAsync_WithDestination_ResolvesAndReturnsIt()
    {
        var dto = new CreateTripDto
        {
            Name = "Tokyo",
            StartDate = Start,
            EndDate = Start,
            DestinationPlaceId = Tokyo.PlaceId,
            PlaceSessionToken = "session-1",
        };

        var result = await _sut.CreateAsync(dto);

        await _placeSearch.Received(1).GetDetailsAsync(Tokyo.PlaceId, "session-1", Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result.Destination!.PlaceId, Is.EqualTo(Tokyo.PlaceId));
            Assert.That(result.Destination.Name, Is.EqualTo("Tokyo"));
            Assert.That(result.Destination.Latitude, Is.EqualTo(35.68));
        });
    }

    [Test]
    public async Task CreateAsync_WithoutDestination_ReturnsNoDestination()
    {
        var result = await _sut.CreateAsync(new CreateTripDto { Name = "Tokyo", StartDate = Start, EndDate = Start });

        Assert.That(result.Destination, Is.Null);
    }

    [Test]
    public async Task CreateAsync_WithUnknownDestination_ThrowsWithoutAdding()
    {
        var dto = new CreateTripDto { Name = "Tokyo", StartDate = Start, EndDate = Start, DestinationPlaceId = "missing" };

        var ex = Assert.ThrowsAsync<DomainException>(() => _sut.CreateAsync(dto));

        Assert.That(ex!.Message, Is.EqualTo("Place not found."));
        await _repository.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithSameDestination_KeepsItWithoutLookingItUp()
    {
        var trip = Trip.Create("Tokyo", Start, Start, Tokyo);
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        var dto = new UpdateTripDto { Name = "Tokyo", StartDate = Start, EndDate = Start, DestinationPlaceId = Tokyo.PlaceId };

        var result = await _sut.UpdateAsync(trip.Id, dto);

        Assert.That(result!.Destination!.PlaceId, Is.EqualTo(Tokyo.PlaceId));
        await _placeSearch.DidNotReceiveWithAnyArgs().GetDetailsAsync(default!, default, default);
    }

    [Test]
    public async Task UpdateAsync_WithoutDestination_ClearsIt()
    {
        var trip = Trip.Create("Tokyo", Start, Start, Tokyo);
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        var dto = new UpdateTripDto { Name = "Tokyo", StartDate = Start, EndDate = Start };

        var result = await _sut.UpdateAsync(trip.Id, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Destination, Is.Null);
            Assert.That(trip.Destination, Is.Null);
        });
    }

    [Test]
    public async Task CreateAsync_WithInvalidDto_DoesNotAddTrip()
    {
        var dto = new CreateTripDto
        {
            Name = "",
            StartDate = Start,
            EndDate = Start,
        };

        Assert.ThrowsAsync<DomainException>(() => _sut.CreateAsync(dto));

        await _repository.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetByIdAsync_ExistingTrip_ReturnsDto()
    {
        var trip = Trip.Create("Tokyo", Start, Start.AddDays(1));
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        var result = await _sut.GetByIdAsync(trip.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(trip.Id));
            Assert.That(result.Name, Is.EqualTo("Tokyo"));
            Assert.That(result.Days, Is.EqualTo(2));
        });
    }

    [Test]
    public void GetByIdAsync_MissingTrip_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Trip?)null);

        Assert.ThrowsAsync<TripNotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetAllAsync_ReturnsDtoForEachTrip()
    {
        var tokyo = Trip.Create("Tokyo", Start, Start);
        var osaka = Trip.Create("Osaka", Start, Start);
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([tokyo, osaka]);

        var result = await _sut.GetAllAsync();

        Assert.That(result.Select(dto => dto.Id), Is.EqualTo(new[] { tokyo.Id, osaka.Id }));
    }

    [Test]
    public async Task DeleteAsync_ExistingTrip_Deletes()
    {
        var trip = Trip.Create("Tokyo", Start, Start);
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        await _sut.DeleteAsync(trip.Id);

        await _repository.Received(1).DeleteAsync(trip, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_MissingTrip_ThrowsWithoutDeleting()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Trip?)null);

        Assert.ThrowsAsync<TripNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithValidDto_UpdatesTripAndSaves()
    {
        var trip = Trip.Create("Tokyo", Start, Start.AddDays(2));
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        var dto = new UpdateTripDto { Name = " Osaka ", StartDate = Start, EndDate = Start.AddDays(4) };

        var result = await _sut.UpdateAsync(trip.Id, dto);

        await _repository.Received(1).UpdateAsync(trip, Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("Osaka"));
            Assert.That(result.EndDate, Is.EqualTo(Start.AddDays(4)));
            Assert.That(result.Days, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task UpdateAsync_WithUnknownTrip_ThrowsWithoutSaving()
    {
        var dto = new UpdateTripDto { Name = "Osaka", StartDate = Start, EndDate = Start };

        Assert.ThrowsAsync<TripNotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), dto));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithActivitiesOutsideNewDates_ThrowsWithoutChangingOrSaving()
    {
        var trip = Trip.Create("Tokyo", Start, Start.AddDays(2));
        trip.AddActivity("Tokyo Tower", Start.AddDays(2), new TimeSlot(new TimeOnly(18, 0), new TimeOnly(19, 0)));
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        var dto = new UpdateTripDto { Name = "Osaka", StartDate = Start, EndDate = Start.AddDays(1) };

        Assert.ThrowsAsync<TripDatesConflictException>(() => _sut.UpdateAsync(trip.Id, dto));

        Assert.Multiple(() =>
        {
            Assert.That(trip.Name, Is.EqualTo("Tokyo"));
            Assert.That(trip.Period.Days, Is.EqualTo(3));
        });
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }
}
