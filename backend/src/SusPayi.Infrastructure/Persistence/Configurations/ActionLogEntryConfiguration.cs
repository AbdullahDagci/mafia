using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SusPayi.Domain.Actions;
using SusPayi.Domain.Characters;

namespace SusPayi.Infrastructure.Persistence.Configurations;

internal sealed class ActionLogEntryConfiguration : IEntityTypeConfiguration<ActionLogEntry>
{
    public void Configure(EntityTypeBuilder<ActionLogEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();
        builder.Property(e => e.Type).HasMaxLength(ActionLogEntry.TypeMaxLength).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(ActionLogEntry.SubjectMaxLength);
        builder.Property(e => e.Outcome).HasMaxLength(ActionLogEntry.OutcomeMaxLength).IsRequired();

        builder.HasOne<Character>().WithMany().HasForeignKey(e => e.CharacterId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.CharacterId, e.OccurredAt });
        builder.HasIndex(e => new { e.SeasonId, e.Type, e.OccurredAt });
    }
}
