using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripWise.Domain;

namespace TripWise.Infrastructure.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public const string TripIdColumn = "TripId";

    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id).ValueGeneratedNever();

        builder.Property(activity => activity.Name)
            .HasMaxLength(Activity.MaxNameLength)
            .IsRequired();

        builder.Property(activity => activity.Date).IsRequired();

        builder.ComplexProperty(activity => activity.TimeSlot, timeSlot =>
        {
            timeSlot.Property(slot => slot.Start).HasColumnName("StartTime");
            timeSlot.Property(slot => slot.End).HasColumnName("EndTime");
        });

        builder.ComplexProperty(activity => activity.Cost, cost =>
        {
            cost.Property(money => money.Amount)
                .HasColumnName("Cost")
                .HasPrecision(12, Money.DecimalPlaces)
                .HasDefaultValue(0m);
        });

        builder.HasIndex(TripIdColumn, nameof(Activity.Date));
    }
}
