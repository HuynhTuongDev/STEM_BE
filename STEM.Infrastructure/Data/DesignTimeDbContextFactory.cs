using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StemDbContext>
{
    public StemDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xvookhjvebxszqfdfuen;Password=OWrRyAs5Vt8p4tG6;SSL Mode=Require;Trust Server Certificate=true";

        var optionsBuilder = new DbContextOptionsBuilder<StemDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new StemDbContext(optionsBuilder.Options);
    }
}
