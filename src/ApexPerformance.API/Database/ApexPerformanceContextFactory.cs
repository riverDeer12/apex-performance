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

        var connectionString = ReadConnectionString(Path.Combine(baseDir, $"appsettings.{environment}.json"))
            ?? ReadConnectionString(Path.Combine(baseDir, "appsettings.json"));

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
