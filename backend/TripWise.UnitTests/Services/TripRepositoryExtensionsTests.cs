using NSubstitute;
using TripWise.Application;
using TripWise.Domain;

namespace TripWise.UnitTests.Services;

public class TripRepositoryExtensionsTests
{
    [Test]
    public async Task GetRequiredAsync_ReturnsTheTrip()
    {
        var repository = Substitute.For<ITripRepository>();
        var trip = Trip.Create("Tokyo", new DateOnly(2021, 5, 1), new DateOnly(2021, 5, 2));
        repository.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        Assert.That(await repository.GetRequiredAsync(trip.Id), Is.SameAs(trip));
    }

    [Test]
    public void GetRequiredAsync_WithUnknownTrip_Throws()
    {
        var repository = Substitute.For<ITripRepository>();
        var tripId = Guid.NewGuid();

        var ex = Assert.ThrowsAsync<TripNotFoundException>(() => repository.GetRequiredAsync(tripId));

        Assert.That(ex!.Message, Is.EqualTo($"Trip {tripId} was not found."));
    }
}
