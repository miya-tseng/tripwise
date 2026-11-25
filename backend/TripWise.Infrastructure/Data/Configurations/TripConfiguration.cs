using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripWise.Domain;

namespace TripWise.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(trip => trip.Id);

        builder.Property(trip => trip.Name)
            .HasMaxLength(Trip.MaxNameLength)
            .IsRequired();

        builder.ComplexProperty(trip => trip.Period, period =>
        {
            period.Property(range => range.Start).HasColumnName("StartDate");
            period.Property(range => range.End).HasColumnName("EndDate");
        });

        builder.HasMany(trip => trip.Activities)
            .WithOne()
            .HasForeignKey(ActivityConfiguration.TripIdColumn)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(trip => trip.Activities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
