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

        builder.ComplexProperty(trip => trip.Destination, place =>
        {
            place.Property(value => value.PlaceId).HasColumnName("DestinationPlaceId").HasMaxLength(Place.MaxPlaceIdLength);
            place.Property(value => value.Name).HasColumnName("DestinationName").HasMaxLength(Place.MaxNameLength);
            place.Property(value => value.Address).HasColumnName("DestinationAddress").HasMaxLength(Place.MaxAddressLength);
            place.Property(value => value.Latitude).HasColumnName("DestinationLatitude");
            place.Property(value => value.Longitude).HasColumnName("DestinationLongitude");
            place.Property(value => value.GoogleMapsUri).HasColumnName("DestinationGoogleMapsUri").HasMaxLength(Place.MaxUriLength);
        });

        builder.HasMany(trip => trip.Activities)
            .WithOne()
            .HasForeignKey(ActivityConfiguration.TripIdColumn)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(trip => trip.Activities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(trip => trip.Stays)
            .WithOne()
            .HasForeignKey(ActivityConfiguration.TripIdColumn)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(trip => trip.Stays)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(trip => trip.Checklist)
            .WithOne()
            .HasForeignKey(ActivityConfiguration.TripIdColumn)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(trip => trip.Checklist)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
