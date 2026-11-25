using System.Globalization;
using Reqnroll;

namespace TripWise.IntegrationTests.Support;

[Binding]
public class Transformations
{
    [StepArgumentTransformation(@"(\d{4}-\d{2}-\d{2})")]
    public DateOnly ToDateOnly(string value)
    {
        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    [StepArgumentTransformation(@"(\d{2}:\d{2})")]
    public TimeOnly ToTimeOnly(string value)
    {
        return TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);
    }
}
