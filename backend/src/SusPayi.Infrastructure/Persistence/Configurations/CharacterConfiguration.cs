using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SusPayi.Domain.Accounts;
using SusPayi.Domain.Characters;
using SusPayi.Domain.Seasons;

namespace SusPayi.Infrastructure.Persistence.Configurations;

internal sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Character.NameMaxLength).IsRequired();
        builder.Ignore(c => c.RankName);

        // PostgreSQL'in sistem kolonu xmin, iyimser eşzamanlılık belirteci olarak kullanılır.
        builder.Property(c => c.Version).IsRowVersion();

        builder.HasOne<Account>().WithMany().HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Season>().WithMany().HasForeignKey(c => c.SeasonId).OnDelete(DeleteBehavior.Restrict);

        // Bir hesabın bir sezonda yalnızca bir canlı karakteri olabilir; ölen karakterin yerine yenisi açılır.
        builder.HasIndex(c => new { c.AccountId, c.SeasonId })
            .IsUnique()
            .HasFilter("\"IsAlive\"");

        // Karakter adı sezon içinde benzersiz.
        builder.HasIndex(c => new { c.SeasonId, c.Name }).IsUnique();

        // Hapishane listesi: sezon içinde hapisteki karakterler, çıkış zamanına göre.
        builder.HasIndex(c => new { c.SeasonId, c.JailedUntil, c.Id })
            .HasFilter("\"JailedUntil\" IS NOT NULL");
    }
}
