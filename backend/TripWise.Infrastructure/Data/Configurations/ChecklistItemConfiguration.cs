using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripWise.Domain;

namespace TripWise.Infrastructure.Data.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.Text)
            .HasMaxLength(ChecklistItem.MaxTextLength)
            .IsRequired();

        builder.Property(item => item.Section)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(ActivityConfiguration.TripIdColumn, nameof(ChecklistItem.Section), nameof(ChecklistItem.Position));
    }
}
