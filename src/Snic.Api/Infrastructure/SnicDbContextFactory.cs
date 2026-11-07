using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Snic.Api.Infrastructure;

public sealed class SnicDbContextFactory : IDesignTimeDbContextFactory<SnicDbContext>
{
    public SnicDbContext CreateDbContext(string[] args)
    {
        EnvFile.LoadForApiProject();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SnicDb")
            ?? "Server=localhost,1433;Database=SNICDB;User Id=sa;Password=DesignTimeOnly!123;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<SnicDbContext>().UseSqlServer(connectionString).Options;
        return new SnicDbContext(options);
    }
}
