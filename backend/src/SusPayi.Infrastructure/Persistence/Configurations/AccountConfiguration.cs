using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SusPayi.Domain.Accounts;

namespace SusPayi.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Email).HasMaxLength(Account.EmailMaxLength).IsRequired();
        builder.Property(a => a.DisplayName).HasMaxLength(Account.DisplayNameMaxLength).IsRequired();
        builder.Property(a => a.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Benzersizlik veritabanında garanti edilir; "önce kontrol et sonra ekle" yarışa açıktır.
        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.DisplayName).IsUnique();
    }
}
