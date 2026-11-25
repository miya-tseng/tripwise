using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripWise.Api.Domain;

namespace TripWise.Api.Data.Configurations;

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

        builder.Ignore(activity => activity.Cost);

        builder.HasIndex(TripIdColumn, nameof(Activity.Date));
    }
}
