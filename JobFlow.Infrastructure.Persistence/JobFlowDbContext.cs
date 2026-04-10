using JobFlow.Domain.Models;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Infrastructure.Persistence;

public class JobFlowDbContext : DbContext
{
    public JobFlowDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Estimate> Estimates { get; set; }
    public DbSet<EstimateLineItem> EstimateLineItems { get; set; }
    public DbSet<EstimateRevisionRequest> EstimateRevisionRequests { get; set; }
    public DbSet<EstimateRevisionAttachment> EstimateRevisionAttachments { get; set; }
    public DbSet<JobUpdate> JobUpdates { get; set; }
    public DbSet<JobUpdateAttachment> JobUpdateAttachments { get; set; }
    public DbSet<InvoiceSequence> InvoiceSequences { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationBranding> OrganizationBrandings { get; set; }
    public DbSet<OrganizationType> OrganizationTypes { get; set; }
    public DbSet<EmployeeRolePreset> EmployeeRolePresets { get; set; }
    public DbSet<EmployeeRolePresetItem> EmployeeRolePresetItems { get; set; }
    public DbSet<JobTemplate> JobTemplates { get; set; }
    public DbSet<JobTemplateItem> JobTemplateItems { get; set; }
    public DbSet<SupportHubTicket> SupportHubTickets { get; set; }
    public DbSet<SupportHubSession> SupportHubSessions { get; set; }
    public DbSet<SupportHubInvite> SupportHubInvites { get; set; }
    public DbSet<FollowUpSequence> FollowUpSequences { get; set; }
    public DbSet<FollowUpStep> FollowUpSteps { get; set; }
    public DbSet<FollowUpRun> FollowUpRuns { get; set; }
    public DbSet<FollowUpExecutionLog> FollowUpExecutionLogs { get; set; }
    public DbSet<ClientImportJob> ClientImportJobs { get; set; }
    public DbSet<ClientImportJobError> ClientImportJobErrors { get; set; }
    public DbSet<ClientImportUploadSession> ClientImportUploadSessions { get; set; }
    public DbSet<ClientImportUploadRow> ClientImportUploadRows { get; set; }
    public DbSet<EmployeeImportJob> EmployeeImportJobs { get; set; }
    public DbSet<EmployeeImportJobError> EmployeeImportJobErrors { get; set; }
    public DbSet<EmployeeImportUploadSession> EmployeeImportUploadSessions { get; set; }
    public DbSet<EmployeeImportUploadRow> EmployeeImportUploadRows { get; set; }
    public DbSet<DataExportJob> DataExportJobs { get; set; }
    public DbSet<HelpArticle> HelpArticles { get; set; }
    public DbSet<ChangelogEntry> ChangelogEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Calling this first prevents EF Core from overriding custom Identity table names
        base.OnModelCreating(modelBuilder);

        // Automatically applies all IEntityTypeConfiguration<T> implementations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobFlowDbContext).Assembly);

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isActiveProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
            var isActiveFilter = Expression.Equal(isActiveProperty, Expression.Constant(true));
            var lambda = Expression.Lambda(isActiveFilter, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}