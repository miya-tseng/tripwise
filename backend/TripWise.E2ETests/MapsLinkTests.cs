using System.Web;
using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class MapsLinkTests : SchedulePageTest
{
    [SetUp]
    public Task RequireFakePlaces() => RequireFakePlacesAsync();

    [Test]
    public async Task PlaceActivity_LinksToItsGoogleMapsPage()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 1, "10:00", "11:00");

        var link = ActivityItem("Senso-ji").GetByTestId("activity-map-link");
        await Expect(link).ToHaveAttributeAsync("href", "https://www.google.com/maps/search/?api=1&query=35.714765,139.796655");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAccessibleNameAsync("Open Senso-ji in Google Maps");
    }

    [Test]
    public async Task CustomActivity_HasNoMapLink()
    {
        await AddActivityAsync("Free time", day: 1, "15:00", "16:00");

        await Expect(ActivityItem("Free time").GetByTestId("activity-map-link")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task ConsecutivePlaces_HaveDirectionsAndADayRoute()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 1, "09:00", "10:30");
        await AddActivityAsync("Lunch break", day: 1, "11:00", "12:00");
        await AddPlaceAsync("skytree", "Tokyo Skytree", day: 1, "13:00", "15:00");
        await AddPlaceAsync("ueno", "Ueno Park", day: 1, "16:00", "17:00");

        var directions = ScheduleDay(1).GetByTestId("directions-link");
        await Expect(directions).ToHaveTextAsync(
            ["Directions from Senso-ji ↗", "Directions from Tokyo Skytree ↗"],
            new() { UseInnerText = true });

        var first = await QueryOfAsync(directions.First);
        Assert.Multiple(() =>
        {
            Assert.That(first["origin_place_id"], Is.EqualTo("fake-senso-ji"));
            Assert.That(first["destination_place_id"], Is.EqualTo("fake-tokyo-skytree"));
            Assert.That(first["origin"], Is.EqualTo("35.714765,139.796655"));
        });

        var route = await QueryOfAsync(ScheduleDay(1).GetByTestId("day-route-link"));
        Assert.Multiple(() =>
        {
            Assert.That(route["origin_place_id"], Is.EqualTo("fake-senso-ji"));
            Assert.That(route["waypoint_place_ids"], Is.EqualTo("fake-tokyo-skytree"));
            Assert.That(route["destination_place_id"], Is.EqualTo("fake-ueno-park"));
        });
    }

    [Test]
    public async Task DayWithOnePlace_HasNoDayRoute()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 2, "10:00", "11:00");

        await Expect(ScheduleDay(2).GetByTestId("day-route-link")).ToHaveCountAsync(0);
        await Expect(ScheduleDay(2).GetByTestId("directions-link")).ToHaveCountAsync(0);
    }

    private static async Task<System.Collections.Specialized.NameValueCollection> QueryOfAsync(ILocator link)
    {
        var href = await link.GetAttributeAsync("href");
        return HttpUtility.ParseQueryString(new Uri(href!).Query);
    }
}
