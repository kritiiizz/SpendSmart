using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpendSmart.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without starting the web host.
/// The connection string is only used for commands that touch the database (e.g. <c>database update</c>).
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SpendSmartDbContext>
{
    public SpendSmartDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=spendsmart;Username=spendsmart;Password=spendsmart";

        var options = new DbContextOptionsBuilder<SpendSmartDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SpendSmartDbContext(options);
    }
}
