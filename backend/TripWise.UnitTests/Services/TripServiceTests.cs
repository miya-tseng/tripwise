using NSubstitute;
using TripWise.Api.Domain;
using TripWise.Api.Dtos;
using TripWise.Api.Services;

namespace TripWise.UnitTests.Services;

public class TripServiceTests
{
    private static readonly DateOnly Start = new(2021, 05, 01);

    private ITripRepository _repository = null!;
    private TripService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ITripRepository>();
        _sut = new TripService(_repository);
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
    public async Task GetByIdAsync_MissingTrip_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Trip?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
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
    public async Task DeleteAsync_ExistingTrip_DeletesAndReturnsTrue()
    {
        var trip = Trip.Create("Tokyo", Start, Start);
        _repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        var deleted = await _sut.DeleteAsync(trip.Id);

        Assert.That(deleted, Is.True);
        await _repository.Received(1).DeleteAsync(trip, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_MissingTrip_ReturnsFalse()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Trip?)null);

        var deleted = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.That(deleted, Is.False);
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }
}
