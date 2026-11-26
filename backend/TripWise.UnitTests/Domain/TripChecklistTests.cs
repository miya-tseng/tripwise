using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripChecklistTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
    }

    private IEnumerable<string> TextsIn(ChecklistSection section) =>
        _trip.Checklist.Where(item => item.Section == section).OrderBy(item => item.Position).Select(item => item.Text);

    private Activity AddActivity(string name, BookingStatus booking) =>
        _trip.AddActivity(
            name,
            FirstDay,
            new TimeSlot(new TimeOnly(10, 0), new TimeOnly(11, 0)),
            details: new ActivityDetails(bookingStatus: booking));

    [Test]
    public void AddChecklistItem_TrimsTextAndAppendsToItsSection()
    {
        _trip.AddChecklistItem("  Passport ", ChecklistSection.ToPack);
        var adapter = _trip.AddChecklistItem("Power adapter", ChecklistSection.ToPack, FirstDay.AddDays(-1));
        _trip.AddChecklistItem("Travel insurance", ChecklistSection.ToBook);

        Assert.Multiple(() =>
        {
            Assert.That(TextsIn(ChecklistSection.ToPack), Is.EqualTo(new[] { "Passport", "Power adapter" }));
            Assert.That(adapter.Position, Is.EqualTo(1));
            Assert.That(adapter.DueDate, Is.EqualTo(FirstDay.AddDays(-1)));
            Assert.That(adapter.Done, Is.False);
            Assert.That(TextsIn(ChecklistSection.ToBook), Is.EqualTo(new[] { "Travel insurance" }));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AddChecklistItem_WithoutText_Throws(string text)
    {
        var ex = Assert.Throws<DomainException>(() => _trip.AddChecklistItem(text, ChecklistSection.Other));

        Assert.That(ex!.Message, Is.EqualTo("Checklist item text is required."));
    }

    [Test]
    public void AddChecklistItem_WithTooLongText_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            _trip.AddChecklistItem(new string('a', ChecklistItem.MaxTextLength + 1), ChecklistSection.Other));

        Assert.That(ex!.Message, Is.EqualTo("Checklist item text cannot exceed 200 characters."));
    }

    [Test]
    public void AddChecklistItem_WithUnknownSection_Throws()
    {
        Assert.Throws<DomainException>(() => _trip.AddChecklistItem("Passport", (ChecklistSection)42));
    }

    [Test]
    public void SetChecklistItemDone_TicksAndUnticks()
    {
        var item = _trip.AddChecklistItem("Passport", ChecklistSection.ToPack);

        _trip.SetChecklistItemDone(item.Id, true);
        Assert.That(item.Done, Is.True);

        _trip.SetChecklistItemDone(item.Id, false);
        Assert.That(item.Done, Is.False);
    }

    [Test]
    public void IsOverdue_OnlyWhenOpenAndPastItsDueDate()
    {
        var item = _trip.AddChecklistItem("Book JR pass", ChecklistSection.ToBook, FirstDay);
        var undated = _trip.AddChecklistItem("Socks", ChecklistSection.ToPack);

        Assert.Multiple(() =>
        {
            Assert.That(item.IsOverdue(FirstDay), Is.False);
            Assert.That(item.IsOverdue(FirstDay.AddDays(1)), Is.True);
            Assert.That(undated.IsOverdue(FirstDay.AddDays(10)), Is.False);
        });

        _trip.SetChecklistItemDone(item.Id, true);
        Assert.That(item.IsOverdue(FirstDay.AddDays(1)), Is.False);
    }

    [Test]
    public void UpdateChecklistItem_ToAnotherSection_MovesItToTheEndAndRenumbers()
    {
        var passport = _trip.AddChecklistItem("Passport", ChecklistSection.ToPack);
        _trip.AddChecklistItem("Charger", ChecklistSection.ToPack);
        _trip.AddChecklistItem("Insurance", ChecklistSection.ToBook);

        _trip.UpdateChecklistItem(passport.Id, "Renew passport", ChecklistSection.ToBook, FirstDay.AddDays(-30));

        Assert.Multiple(() =>
        {
            Assert.That(TextsIn(ChecklistSection.ToBook), Is.EqualTo(new[] { "Insurance", "Renew passport" }));
            Assert.That(TextsIn(ChecklistSection.ToPack), Is.EqualTo(new[] { "Charger" }));
            Assert.That(_trip.Checklist.Single(item => item.Text == "Charger").Position, Is.Zero);
            Assert.That(passport.DueDate, Is.EqualTo(FirstDay.AddDays(-30)));
        });
    }

    [Test]
    public void RemoveChecklistItem_RemovesAndRenumbers()
    {
        var passport = _trip.AddChecklistItem("Passport", ChecklistSection.ToPack);
        var charger = _trip.AddChecklistItem("Charger", ChecklistSection.ToPack);

        _trip.RemoveChecklistItem(passport.Id);

        Assert.Multiple(() =>
        {
            Assert.That(_trip.Checklist, Is.EqualTo(new[] { charger }));
            Assert.That(charger.Position, Is.Zero);
        });
    }

    [Test]
    public void ChecklistItemOperations_WithUnknownItem_Throw()
    {
        var unknown = Guid.NewGuid();

        Assert.Multiple(() =>
        {
            Assert.Throws<ChecklistItemNotFoundException>(() => _trip.SetChecklistItemDone(unknown, true));
            Assert.Throws<ChecklistItemNotFoundException>(() => _trip.RemoveChecklistItem(unknown));
            Assert.Throws<ChecklistItemNotFoundException>(() =>
                _trip.UpdateChecklistItem(unknown, "x", ChecklistSection.Other, null));
        });
    }

    [Test]
    public void ReorderChecklist_SetsTheNewOrder()
    {
        var a = _trip.AddChecklistItem("A", ChecklistSection.ToPack);
        var b = _trip.AddChecklistItem("B", ChecklistSection.ToPack);
        var c = _trip.AddChecklistItem("C", ChecklistSection.ToPack);

        _trip.ReorderChecklist(ChecklistSection.ToPack, [c.Id, a.Id, b.Id]);

        Assert.That(TextsIn(ChecklistSection.ToPack), Is.EqualTo(new[] { "C", "A", "B" }));
    }

    [Test]
    public void ReorderChecklist_WithMissingOrForeignItems_Throws()
    {
        var a = _trip.AddChecklistItem("A", ChecklistSection.ToPack);
        var b = _trip.AddChecklistItem("B", ChecklistSection.ToPack);
        var other = _trip.AddChecklistItem("Other", ChecklistSection.Other);

        Assert.Multiple(() =>
        {
            Assert.Throws<DomainException>(() => _trip.ReorderChecklist(ChecklistSection.ToPack, [a.Id]));
            Assert.Throws<DomainException>(() => _trip.ReorderChecklist(ChecklistSection.ToPack, [a.Id, other.Id]));
            Assert.Throws<DomainException>(() => _trip.ReorderChecklist(ChecklistSection.ToPack, [a.Id, a.Id]));
        });
        Assert.That(TextsIn(ChecklistSection.ToPack), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(b.Position, Is.EqualTo(1));
    }

    [Test]
    public void SetActivityBooked_TicksToBookedAndUntickBackToToBook()
    {
        var teamLab = AddActivity("teamLab", BookingStatus.ToBook);

        _trip.SetActivityBooked(teamLab.Id, true);
        Assert.That(teamLab.Details.BookingStatus, Is.EqualTo(BookingStatus.Booked));

        _trip.SetActivityBooked(teamLab.Id, false);
        Assert.That(teamLab.Details.BookingStatus, Is.EqualTo(BookingStatus.ToBook));
    }

    [Test]
    public void SetActivityBooked_KeepsPaidWhenTickedAgainAndKeepsOtherDetails()
    {
        var sushi = _trip.AddActivity(
            "Sushi",
            FirstDay,
            new TimeSlot(new TimeOnly(7, 0), new TimeOnly(8, 0)),
            details: new ActivityDetails(ActivityCategory.Food, BookingStatus.Paid, "Cash only", "SD-1"));

        _trip.SetActivityBooked(sushi.Id, true);

        Assert.Multiple(() =>
        {
            Assert.That(sushi.Details.BookingStatus, Is.EqualTo(BookingStatus.Paid));
            Assert.That(sushi.Details.Category, Is.EqualTo(ActivityCategory.Food));
            Assert.That(sushi.Details.Notes, Is.EqualTo("Cash only"));
            Assert.That(sushi.Details.ConfirmationCode, Is.EqualTo("SD-1"));
        });
    }

    [Test]
    public void SetActivityBooked_ForAnActivityWithoutBooking_Throws()
    {
        var walk = AddActivity("Walk", BookingStatus.NotNeeded);

        var ex = Assert.Throws<DomainException>(() => _trip.SetActivityBooked(walk.Id, true));

        Assert.That(ex!.Message, Is.EqualTo("This item does not need booking."));
    }

    [Test]
    public void SetStayBooked_TicksAndUnticks()
    {
        var inn = _trip.AddStay(new StayDetails("Inn", FirstDay, FirstDay.AddDays(1), bookingStatus: BookingStatus.ToBook));

        _trip.SetStayBooked(inn.Id, true);
        Assert.That(inn.BookingStatus, Is.EqualTo(BookingStatus.Booked));

        _trip.SetStayBooked(inn.Id, false);
        Assert.That(inn.BookingStatus, Is.EqualTo(BookingStatus.ToBook));
    }
}
