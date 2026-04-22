using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FMCPA.Infrastructure.Persistence.DesignTime;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    private const string DefaultConnectionString =
        "Server=localhost,1433;Database=FMCPA_DesignTime;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True";

    public PlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PlatformDatabase");

        optionsBuilder.UseSqlServer(
            string.IsNullOrWhiteSpace(connectionString) ? DefaultConnectionString : connectionString);

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
