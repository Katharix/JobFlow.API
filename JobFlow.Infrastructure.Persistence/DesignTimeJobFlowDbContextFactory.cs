using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobFlow.Infrastructure.Persistence;

public sealed class DesignTimeJobFlowDbContextFactory : IDesignTimeDbContextFactory<JobFlowDbContext>
{
    public JobFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__JobFlowDB");

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable("JobFlowDB");

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Missing DB connection string for EF design-time operations. Set one of: ConnectionStrings__JobFlowDB, JobFlowDB, or SqlConnectionString.");

        var optionsBuilder = new DbContextOptionsBuilder<JobFlowDbContext>();
        optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly("JobFlow.Infrastructure.Persistence"));

        return new JobFlowDbContext(optionsBuilder.Options);
    }
}