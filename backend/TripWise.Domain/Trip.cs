namespace TripWise.Domain;

public class Trip
{
    public const int MaxNameLength = 100;
    public const int MaxDays = 30;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public Place? Destination { get; private set; }

    private readonly List<Activity> _activities = [];
    public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();

    private readonly List<Stay> _stays = [];
    public IReadOnlyCollection<Stay> Stays => _stays.AsReadOnly();

    private readonly List<ChecklistItem> _checklist = [];
    public IReadOnlyCollection<ChecklistItem> Checklist => _checklist.AsReadOnly();

    public DateOnly LastCheckOut => Period.End.AddDays(1);

    public IReadOnlyList<DateOnly> Nights =>
        Enumerable.Range(0, Period.Days).Select(Period.Start.AddDays).ToList();

    private Trip() { }

    public Activity AddActivity(
        string name,
        DateOnly date,
        TimeSlot timeSlot,
        Money? cost = null,
        Place? place = null,
        ActivityDetails? details = null)
    {
        var normalizedName = Activity.NormalizeName(name, place);
        EnsureActivityFits(date, timeSlot, excludedActivityId: null);

        var activity = Activity.Create(
            normalizedName,
            date,
            timeSlot,
            cost ?? Money.Zero,
            place,
            details ?? ActivityDetails.Default);
        _activities.Add(activity);
        return activity;
    }

    public Activity UpdateActivity(
        Guid activityId,
        string name,
        DateOnly date,
        TimeSlot timeSlot,
        Money? cost = null,
        Place? place = null,
        ActivityDetails? details = null)
    {
        var activity = FindActivity(activityId);
        var normalizedName = Activity.NormalizeName(name, place);
        EnsureActivityFits(date, timeSlot, excludedActivityId: activityId);

        activity.Update(normalizedName, date, timeSlot, cost ?? Money.Zero, place, details ?? ActivityDetails.Default);
        return activity;
    }

    public Activity MoveActivity(Guid activityId, DateOnly date, TimeOnly startTime)
    {
        var activity = FindActivity(activityId);
        var timeSlot = activity.TimeSlot.StartingAt(startTime);
        EnsureActivityFits(date, timeSlot, excludedActivityId: activityId);

        activity.MoveTo(date, timeSlot);
        return activity;
    }

    public Stay AddStay(StayDetails details)
    {
        EnsureStayFits(details, excludedStayId: null);

        var stay = Stay.Create(details);
        _stays.Add(stay);
        return stay;
    }

    public Stay UpdateStay(Guid stayId, StayDetails details)
    {
        var stay = FindStay(stayId);
        EnsureStayFits(details, excludedStayId: stayId);

        stay.Update(details);
        return stay;
    }

    public void RemoveStay(Guid stayId)
    {
        _stays.Remove(FindStay(stayId));
    }

    public Stay? StayForNight(DateOnly night) => _stays.FirstOrDefault(stay => stay.CoversNight(night));

    private Stay FindStay(Guid stayId) =>
        _stays.FirstOrDefault(stay => stay.Id == stayId)
        ?? throw new StayNotFoundException(stayId);

    private void EnsureStayFits(StayDetails details, Guid? excludedStayId)
    {
        if (details.CheckIn < Period.Start || details.CheckOut > LastCheckOut)
        {
            throw new DomainException("Stay dates must be within the trip dates.");
        }

        var conflictingStay = _stays.FirstOrDefault(stay =>
            stay.Id != excludedStayId && stay.OverlapsWith(details.CheckIn, details.CheckOut));

        if (conflictingStay is not null)
        {
            throw new StayOverlapException(conflictingStay);
        }
    }

    public ChecklistItem AddChecklistItem(string text, ChecklistSection section, DateOnly? dueDate = null)
    {
        var position = _checklist.Where(item => item.Section == section).Select(item => item.Position + 1).DefaultIfEmpty(0).Max();
        var item = ChecklistItem.Create(text, section, dueDate, position);
        _checklist.Add(item);
        return item;
    }

    public ChecklistItem UpdateChecklistItem(Guid itemId, string text, ChecklistSection section, DateOnly? dueDate)
    {
        var item = FindChecklistItem(itemId);
        var previousSection = item.Section;
        item.Update(text, section, dueDate);

        if (previousSection != section)
        {
            item.MoveTo(section, ItemsIn(section).Where(other => other.Id != itemId).Select(other => other.Position + 1).DefaultIfEmpty(0).Max());
            Renumber(previousSection);
        }

        return item;
    }

    public ChecklistItem SetChecklistItemDone(Guid itemId, bool done)
    {
        var item = FindChecklistItem(itemId);
        item.MarkDone(done);
        return item;
    }

    public void RemoveChecklistItem(Guid itemId)
    {
        var item = FindChecklistItem(itemId);
        _checklist.Remove(item);
        Renumber(item.Section);
    }

    public void ReorderChecklist(ChecklistSection section, IReadOnlyList<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        var items = ItemsIn(section);

        if (itemIds.Count != items.Count || itemIds.Distinct().Count() != items.Count || items.Any(item => !itemIds.Contains(item.Id)))
        {
            throw new DomainException("The new order must list every item of the section once.");
        }

        for (var index = 0; index < itemIds.Count; index++)
        {
            items.First(item => item.Id == itemIds[index]).MoveTo(section, index);
        }
    }

    public Activity SetActivityBooked(Guid activityId, bool booked)
    {
        var activity = FindActivity(activityId);
        EnsureNeedsBooking(activity.Details.BookingStatus);
        activity.ChangeBookingStatus(NextBookingStatus(activity.Details.BookingStatus, booked));
        return activity;
    }

    public Stay SetStayBooked(Guid stayId, bool booked)
    {
        var stay = FindStay(stayId);
        EnsureNeedsBooking(stay.BookingStatus);
        stay.ChangeBookingStatus(NextBookingStatus(stay.BookingStatus, booked));
        return stay;
    }

    private static void EnsureNeedsBooking(BookingStatus status)
    {
        if (status == BookingStatus.NotNeeded)
        {
            throw new DomainException("This item does not need booking.");
        }
    }

    private static BookingStatus NextBookingStatus(BookingStatus current, bool booked) => booked
        ? current == BookingStatus.ToBook ? BookingStatus.Booked : current
        : BookingStatus.ToBook;

    private List<ChecklistItem> ItemsIn(ChecklistSection section) =>
        _checklist.Where(item => item.Section == section).OrderBy(item => item.Position).ToList();

    private void Renumber(ChecklistSection section)
    {
        var items = ItemsIn(section);

        for (var index = 0; index < items.Count; index++)
        {
            items[index].MoveTo(section, index);
        }
    }

    private ChecklistItem FindChecklistItem(Guid itemId) =>
        _checklist.FirstOrDefault(item => item.Id == itemId)
        ?? throw new ChecklistItemNotFoundException(itemId);

    public void RemoveActivity(Guid activityId)
    {
        _activities.Remove(FindActivity(activityId));
    }

    public IReadOnlyList<Money> GetDailyTotals(DateOnly date) => Money.SumByCurrency(GetDailyCosts(date));

    public IReadOnlyList<Money> GetTotals() => Money.SumByCurrency(GetAllCosts());

    public IEnumerable<Money> GetDailyCosts(DateOnly date) =>
        _activities
            .Where(activity => activity.Date == date)
            .Select(activity => activity.Cost)
            .Concat(PaidStays.Where(stay => stay.CoversNight(date)).Select(stay => stay.CostForNight(date)));

    public IEnumerable<Money> GetAllCosts() =>
        _activities.Select(activity => activity.Cost).Concat(PaidStays.Select(stay => stay.Cost));

    private IEnumerable<Stay> PaidStays => _stays.Where(stay => stay.Cost.Amount > 0);

    private Activity FindActivity(Guid activityId) =>
        _activities.FirstOrDefault(activity => activity.Id == activityId)
        ?? throw new ActivityNotFoundException(activityId);

    private void EnsureActivityFits(DateOnly date, TimeSlot timeSlot, Guid? excludedActivityId)
    {
        ArgumentNullException.ThrowIfNull(timeSlot);

        if (!Period.Contains(date))
        {
            throw new DomainException("Activity date must be within the trip dates.");
        }

        var conflictingActivity = _activities.FirstOrDefault(activity =>
            activity.Id != excludedActivityId && activity.ConflictsWith(date, timeSlot));

        if (conflictingActivity is not null)
        {
            throw new ActivityOverlapException(conflictingActivity);
        }
    }

    public static Trip Create(string name, DateOnly start, DateOnly end, Place? destination = null)
    {
        return new Trip
        {
            Id = Guid.CreateVersion7(),
            Name = NormalizeName(name),
            Period = CreatePeriod(start, end),
            Destination = destination,
        };
    }

    public void ChangeDestination(Place? destination)
    {
        Destination = destination;
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Reschedule(DateOnly start, DateOnly end)
    {
        var period = CreatePeriod(start, end);
        var activitiesOutside = _activities.Count(activity => !period.Contains(activity.Date));

        if (activitiesOutside > 0)
        {
            throw new TripDatesConflictException(activitiesOutside);
        }

        if (_stays.Any(stay => stay.CheckIn < period.Start || stay.CheckOut > period.End.AddDays(1)))
        {
            throw new DomainException("A stay falls outside the new dates. Change or delete it first.");
        }

        Period = period;
    }

    private static string NormalizeName(string name)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            throw new DomainException("Trip name is required.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Trip name cannot exceed {MaxNameLength} characters.");
        }

        return trimmedName;
    }

    private static DateRange CreatePeriod(DateOnly start, DateOnly end)
    {
        var period = new DateRange(start, end);

        if (period.Days > MaxDays)
        {
            throw new DomainException($"A trip cannot be longer than {MaxDays} days.");
        }

        return period;
    }
}
