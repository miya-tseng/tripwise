using TripWise.Api.Domain;

namespace TripWise.UnitTests.Domain;

public class TimeSlotTests
{
    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void Constructor_SetsStartAndEnd()
    {
        var slot = Slot("09:00", "10:30");

        Assert.Multiple(() =>
        {
            Assert.That(slot.Start, Is.EqualTo(new TimeOnly(9, 0)));
            Assert.That(slot.End, Is.EqualTo(new TimeOnly(10, 30)));
        });
    }

    [TestCase("10:00", "10:00")]
    [TestCase("11:00", "10:00")]
    public void Constructor_WithEndNotAfterStart_ThrowsDomainException(string start, string end)
    {
        var ex = Assert.Throws<DomainException>(() => Slot(start, end));

        Assert.That(ex.Message, Is.EqualTo("End time must be later than start time."));
    }

    [Test]
    public void OverlapsWith_SameSlot_ReturnsTrue()
    {
        var slot = Slot("10:00", "11:00");

        Assert.That(slot.OverlapsWith(Slot("10:00", "11:00")), Is.True);
    }

    [TestCase("09:30", "10:30")]
    [TestCase("10:30", "11:30")]
    public void OverlapsWith_PartialOverlap_ReturnsTrue(string otherStart, string otherEnd)
    {
        var slot = Slot("10:00", "11:00");

        Assert.That(slot.OverlapsWith(Slot(otherStart, otherEnd)), Is.True);
    }

    [Test]
    public void OverlapsWith_OtherContainedInside_ReturnsTrue()
    {
        var slot = Slot("10:00", "12:00");

        Assert.That(slot.OverlapsWith(Slot("10:30", "11:30")), Is.True);
    }

    [Test]
    public void OverlapsWith_OtherContainingThis_ReturnsTrue()
    {
        var slot = Slot("10:30", "11:30");

        Assert.That(slot.OverlapsWith(Slot("10:00", "12:00")), Is.True);
    }

    [TestCase("09:00", "10:00")]
    [TestCase("11:00", "12:00")]
    public void OverlapsWith_BackToBack_ReturnsFalse(string otherStart, string otherEnd)
    {
        var slot = Slot("10:00", "11:00");

        Assert.That(slot.OverlapsWith(Slot(otherStart, otherEnd)), Is.False);
    }

    [TestCase("07:00", "08:00")]
    [TestCase("13:00", "14:00")]
    public void OverlapsWith_Separate_ReturnsFalse(string otherStart, string otherEnd)
    {
        var slot = Slot("10:00", "11:00");

        Assert.That(slot.OverlapsWith(Slot(otherStart, otherEnd)), Is.False);
    }

    [TestCase("09:30", "10:30")]
    [TestCase("11:00", "12:00")]
    public void OverlapsWith_IsSymmetric(string otherStart, string otherEnd)
    {
        var slot = Slot("10:00", "11:00");
        var other = Slot(otherStart, otherEnd);

        Assert.That(slot.OverlapsWith(other), Is.EqualTo(other.OverlapsWith(slot)));
    }

    [Test]
    public void Equals_WithSameTimes_ReturnsTrue()
    {
        var a = Slot("10:00", "11:00");
        var b = Slot("10:00", "11:00");

        Assert.That(a, Is.EqualTo(b));
    }
}
