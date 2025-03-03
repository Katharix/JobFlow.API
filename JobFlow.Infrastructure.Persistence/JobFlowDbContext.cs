using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;

namespace JobFlow.Infrastructure.Persistence
{
    public class JobFlowDbContext : DbContext
    {
        public JobFlowDbContext(DbContextOptions options) : base(options)
        {        
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationType> OrganizationTypes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Automatically applies all IEntityTypeConfiguration<T> implementations in the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobFlowDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

    }
}
