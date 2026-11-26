using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripWise.Domain;

namespace TripWise.Infrastructure.Data.Configurations;

public class StayConfiguration : IEntityTypeConfiguration<Stay>
{
    public void Configure(EntityTypeBuilder<Stay> builder)
    {
        builder.ToTable("Stays");

        builder.HasKey(stay => stay.Id);

        builder.Property(stay => stay.Id).ValueGeneratedNever();

        builder.Property(stay => stay.Name)
            .HasMaxLength(Stay.MaxNameLength)
            .IsRequired();

        builder.Property(stay => stay.CheckIn).IsRequired();
        builder.Property(stay => stay.CheckOut).IsRequired();

        builder.ComplexProperty(stay => stay.Cost, cost =>
        {
            cost.Property(money => money.Amount)
                .HasColumnName("Cost")
                .HasPrecision(12, Money.DecimalPlaces)
                .HasDefaultValue(0m);

            cost.Property(money => money.Currency)
                .HasColumnName("CostCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .HasDefaultValue(Money.HomeCurrency);
        });

        builder.ComplexProperty(stay => stay.Place, place =>
        {
            place.Property(value => value.PlaceId).HasColumnName("PlaceId").HasMaxLength(Place.MaxPlaceIdLength);
            place.Property(value => value.Name).HasColumnName("PlaceName").HasMaxLength(Place.MaxNameLength);
            place.Property(value => value.Address).HasColumnName("PlaceAddress").HasMaxLength(Place.MaxAddressLength);
            place.Property(value => value.Latitude).HasColumnName("PlaceLatitude");
            place.Property(value => value.Longitude).HasColumnName("PlaceLongitude");
            place.Property(value => value.GoogleMapsUri).HasColumnName("PlaceGoogleMapsUri").HasMaxLength(Place.MaxUriLength);
        });

        builder.Property(stay => stay.BookingStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(BookingStatus.NotNeeded);

        builder.Property(stay => stay.ConfirmationCode)
            .HasMaxLength(ActivityDetails.MaxConfirmationCodeLength)
            .HasDefaultValue(string.Empty);

        builder.HasIndex(ActivityConfiguration.TripIdColumn, nameof(Stay.CheckIn));
    }
}
