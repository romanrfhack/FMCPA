using FMCPA.Application.Abstractions.Persistence;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Infrastructure.Persistence;
using FMCPA.Infrastructure.Storage.Donations;
using FMCPA.Infrastructure.Storage.Federation;
using FMCPA.Infrastructure.Storage.Markets;
using FMCPA.Infrastructure.Storage.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FMCPA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PlatformDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The connection string 'PlatformDatabase' is required.");
        }

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)));

        services.AddScoped<IPlatformDbContext>(provider => provider.GetRequiredService<PlatformDbContext>());
        services.AddScoped<IDocumentBinaryStore, LocalDocumentBinaryStore>();
        services.AddScoped<IMarketTenantCertificateStorage, MarketTenantCertificateStorage>();
        services.AddScoped<IDonationApplicationEvidenceStorage, DonationApplicationEvidenceStorage>();
        services.AddScoped<IFederationDonationApplicationEvidenceStorage, FederationDonationApplicationEvidenceStorage>();

        return services;
    }
}
