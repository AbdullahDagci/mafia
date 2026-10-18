using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SusPayi.Infrastructure.Persistence;

/// <summary>
/// "dotnet ef migrations add" komutunun API'yi başlatmadan modeli kurabilmesi için.
/// Bağlantı dizesi yalnızca migration üretiminde kullanılır; veritabanına bağlanılmaz.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SusPayiDbContext>
{
    public SusPayiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SusPayiDbContext>()
            .UseNpgsql("Host=localhost;Database=suspayi_design;Username=design;Password=design")
            .Options;
        return new SusPayiDbContext(options);
    }
}
