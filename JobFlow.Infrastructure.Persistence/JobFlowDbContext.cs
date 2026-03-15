using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Infrastructure.Persistence;

public class JobFlowDbContext : DbContext
{
    public JobFlowDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Estimate> Estimates { get; set; }
    public DbSet<EstimateLineItem> EstimateLineItems { get; set; }
    public DbSet<InvoiceSequence> InvoiceSequences { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationType> OrganizationTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Calling this first prevents EF Core from overriding custom Identity table names
        base.OnModelCreating(modelBuilder);

        // Automatically applies all IEntityTypeConfiguration<T> implementations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobFlowDbContext).Assembly);
    }
}