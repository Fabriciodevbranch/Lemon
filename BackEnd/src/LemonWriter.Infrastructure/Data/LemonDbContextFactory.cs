using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LemonWriter.Infrastructure.Data;

public class LemonDbContextFactory : IDesignTimeDbContextFactory<LemonDbContext>
{
    public LemonDbContext CreateDbContext(string[] args)
    {
        var basePath = FindApiProjectDirectory() ?? Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Ensure appsettings.json exists in the LemonWriter.API project directory " +
                "or set the ConnectionStrings__DefaultConnection environment variable.");

        var optionsBuilder = new DbContextOptionsBuilder<LemonDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LemonDbContext(optionsBuilder.Options);
    }

    private static string? FindApiProjectDirectory()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "src", "LemonWriter.API");
            if (Directory.Exists(candidate))
                return candidate;

            candidate = Path.Combine(current.FullName, "LemonWriter.API");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }
        return null;
    }
}
