using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class ActivityDetailsTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void Default_IsOtherAndNotNeededWithoutText()
    {
        var details = ActivityDetails.Default;

        Assert.Multiple(() =>
        {
            Assert.That(details.Category, Is.EqualTo(ActivityCategory.Other));
            Assert.That(details.BookingStatus, Is.EqualTo(BookingStatus.NotNeeded));
            Assert.That(details.Notes, Is.Empty);
            Assert.That(details.ConfirmationCode, Is.Empty);
            Assert.That(details.Link, Is.Null);
            Assert.That(details.NeedsBooking, Is.False);
        });
    }

    [Test]
    public void Constructor_KeepsTrimmedValues()
    {
        var details = new ActivityDetails(
            ActivityCategory.Sightseeing,
            BookingStatus.Booked,
            "  Bring the QR code  ",
            "  AB-123  ",
            "  https://www.shibuya-scramble-square.com/sky/  ");

        Assert.Multiple(() =>
        {
            Assert.That(details.Category, Is.EqualTo(ActivityCategory.Sightseeing));
            Assert.That(details.BookingStatus, Is.EqualTo(BookingStatus.Booked));
            Assert.That(details.Notes, Is.EqualTo("Bring the QR code"));
            Assert.That(details.ConfirmationCode, Is.EqualTo("AB-123"));
            Assert.That(details.Link, Is.EqualTo("https://www.shibuya-scramble-square.com/sky/"));
        });
    }

    [Test]
    public void NeedsBooking_OnlyWhenToBook()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new ActivityDetails(bookingStatus: BookingStatus.ToBook).NeedsBooking, Is.True);
            Assert.That(new ActivityDetails(bookingStatus: BookingStatus.Booked).NeedsBooking, Is.False);
            Assert.That(new ActivityDetails(bookingStatus: BookingStatus.Paid).NeedsBooking, Is.False);
        });
    }

    [Test]
    public void Constructor_WithNotesAtMaxLength_IsAllowed()
    {
        var notes = new string('a', ActivityDetails.MaxNotesLength);

        Assert.That(new ActivityDetails(notes: notes).Notes, Has.Length.EqualTo(ActivityDetails.MaxNotesLength));
    }

    [Test]
    public void Constructor_WithTooLongNotes_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            new ActivityDetails(notes: new string('a', ActivityDetails.MaxNotesLength + 1)));

        Assert.That(ex.Message, Is.EqualTo("Notes cannot exceed 2000 characters."));
    }

    [Test]
    public void Constructor_WithTooLongConfirmationCode_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            new ActivityDetails(confirmationCode: new string('A', ActivityDetails.MaxConfirmationCodeLength + 1)));

        Assert.That(ex.Message, Is.EqualTo("Confirmation code cannot exceed 100 characters."));
    }

    [TestCase("http://example.com")]
    [TestCase("not a link")]
    [TestCase("javascript:alert(1)")]
    [TestCase("ftp://example.com/file")]
    public void Constructor_WithInvalidLink_Throws(string link)
    {
        var ex = Assert.Throws<DomainException>(() => new ActivityDetails(link: link));

        Assert.That(ex.Message, Is.EqualTo("Link must be an https URL."));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Constructor_WithBlankLink_HasNoLink(string? link)
    {
        Assert.That(new ActivityDetails(link: link).Link, Is.Null);
    }

    [Test]
    public void Constructor_WithUnknownCategory_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => new ActivityDetails((ActivityCategory)99));

        Assert.That(ex.Message, Is.EqualTo("Unknown activity category."));
    }

    [Test]
    public void Constructor_WithUnknownBookingStatus_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => new ActivityDetails(bookingStatus: (BookingStatus)99));

        Assert.That(ex.Message, Is.EqualTo("Unknown booking status."));
    }

    [Test]
    public void AddActivity_WithoutDetails_UsesDefault()
    {
        var trip = Trip.Create("Tokyo", FirstDay, FirstDay);

        var activity = trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        Assert.That(activity.Details, Is.EqualTo(ActivityDetails.Default));
    }

    [Test]
    public void AddAndUpdateActivity_KeepDetails()
    {
        var trip = Trip.Create("Tokyo", FirstDay, FirstDay);
        var toBook = new ActivityDetails(ActivityCategory.Experience, BookingStatus.ToBook);
        var activity = trip.AddActivity("teamLab", FirstDay, Slot("10:00", "11:00"), details: toBook);
        Assert.That(activity.Details, Is.EqualTo(toBook));

        var paid = new ActivityDetails(ActivityCategory.Experience, BookingStatus.Paid, confirmationCode: "TL-9");
        trip.UpdateActivity(activity.Id, "teamLab", FirstDay, Slot("10:00", "11:00"), details: paid);

        Assert.That(activity.Details, Is.EqualTo(paid));
    }

    [Test]
    public void MoveActivity_KeepsDetails()
    {
        var trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(1));
        var details = new ActivityDetails(ActivityCategory.Food, BookingStatus.Booked, "Window seat");
        var activity = trip.AddActivity("Sushi", FirstDay, Slot("12:00", "13:00"), details: details);

        trip.MoveActivity(activity.Id, FirstDay.AddDays(1), new TimeOnly(12, 0));

        Assert.That(activity.Details, Is.EqualTo(details));
    }
}
