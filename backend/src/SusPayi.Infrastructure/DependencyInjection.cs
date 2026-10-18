using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SusPayi.Domain.Rules;
using SusPayi.Infrastructure.Persistence;
using SusPayi.Infrastructure.Randomness;

namespace SusPayi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Game");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Game ayarı eksik. Üretimde ortam değişkeni ConnectionStrings__Game ile verilir.");
        }

        services.AddDbContext<SusPayiDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

        services.AddSingleton<IRandomSource, CryptoRandomSource>();
        return services;
    }
}
