using System.Text.RegularExpressions;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApexPerformance.API.Database;

public class ApexPerformanceContextFactory : IDesignTimeDbContextFactory<ApexPerformanceContext>
{
    private class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid UserId => Guid.Empty;
        public bool LoggedUserHasRole(string requiredRole) => false;
    }

    public ApexPerformanceContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var baseDir = AppContext.BaseDirectory;

        // Falls back to a placeholder when no local appsettings file is present (e.g. in CI,
        // where appsettings*.json are gitignored) - `dotnet ef ... --connection` overrides this
        // after the context is constructed, so the placeholder itself is never used to connect.
        var connectionString = ReadConnectionString(Path.Combine(baseDir, $"appsettings.{environment}.json"))
            ?? ReadConnectionString(Path.Combine(baseDir, "appsettings.json"))
            ?? "Server=.;Database=Undefined;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<ApexPerformanceContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApexPerformanceContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    private static string? ReadConnectionString(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var match = Regex.Match(File.ReadAllText(path), "\"DefaultConnection\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        return match.Success ? Regex.Unescape(match.Groups[1].Value) : null;
    }
}
