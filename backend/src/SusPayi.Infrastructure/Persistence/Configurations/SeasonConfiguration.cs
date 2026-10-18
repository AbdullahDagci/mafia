using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SusPayi.Domain.Seasons;

namespace SusPayi.Infrastructure.Persistence.Configurations;

internal sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(Season.NameMaxLength).IsRequired();
        builder.HasIndex(s => s.Number).IsUnique();
        builder.HasIndex(s => new { s.StartsAt, s.EndsAt });

        // Sezon ayarları aynı tabloda, "Rules_" önekli kolonlarda tutulur.
        builder.OwnsOne(s => s.Rules);
        builder.Navigation(s => s.Rules).IsRequired();
    }
}
