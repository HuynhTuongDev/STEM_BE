using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StemDbContext>
{
    public StemDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StemDbContext>();

        // Sử dụng connection string mặc định hoặc từ environment variable
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Server=localhost;Database=STEM;User Id=sa;Password=Password123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString);

        return new StemDbContext(optionsBuilder.Options);
    }
}
