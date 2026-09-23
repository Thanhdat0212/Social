using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public class SocialDbContextFactory : IDesignTimeDbContextFactory<SocialDbContext>
{
    public SocialDbContext CreateDbContext(string[] args)
    {
        // Try reading configuration from API directory or current directory
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../API");
        if (!Directory.Exists(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<SocialDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=social_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<SocialDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName));

        return new SocialDbContext(optionsBuilder.Options);
    }
}
