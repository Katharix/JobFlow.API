using JobFlow.API.Models;
using JobFlow.API.Services;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class EmployeeImportCsvServiceTests
{
    [Fact]
    public void BuildSuggestedMappings_MapsKnownHeaders()
    {
        var headers = new[] { "First Name", "Last Name", "Email Address", "Phone Number" };

        var mappings = EmployeeImportCsvService.BuildSuggestedMappings(headers);

        Assert.Equal(EmployeeImportTargetFields.FirstName, mappings["First Name"]);
        Assert.Equal(EmployeeImportTargetFields.LastName, mappings["Last Name"]);
        Assert.Equal(EmployeeImportTargetFields.Email, mappings["Email Address"]);
        Assert.Equal(EmployeeImportTargetFields.PhoneNumber, mappings["Phone Number"]);
    }

    [Fact]
    public void BuildSuggestedMappings_MapsExactKeywords()
    {
        var headers = new[] { "firstname", "surname", "mail", "mobile" };

        var mappings = EmployeeImportCsvService.BuildSuggestedMappings(headers);

        Assert.Equal(EmployeeImportTargetFields.FirstName, mappings["firstname"]);
        Assert.Equal(EmployeeImportTargetFields.LastName, mappings["surname"]);
        Assert.Equal(EmployeeImportTargetFields.Email, mappings["mail"]);
        Assert.Equal(EmployeeImportTargetFields.PhoneNumber, mappings["mobile"]);
    }

    [Fact]
    public void BuildSuggestedMappings_IgnoresUnknownHeaders()
    {
        var headers = new[] { "Department", "Hire Date", "Notes" };

        var mappings = EmployeeImportCsvService.BuildSuggestedMappings(headers);

        Assert.Equal(EmployeeImportTargetFields.Ignore, mappings["Department"]);
        Assert.Equal(EmployeeImportTargetFields.Ignore, mappings["Hire Date"]);
        Assert.Equal(EmployeeImportTargetFields.Ignore, mappings["Notes"]);
    }

    [Fact]
    public void BuildSuggestedMappings_MapsFullName()
    {
        var headers = new[] { "Full Name", "EmployeeName" };

        var mappings = EmployeeImportCsvService.BuildSuggestedMappings(headers);

        Assert.Equal(EmployeeImportTargetFields.FullName, mappings["Full Name"]);
        Assert.Equal(EmployeeImportTargetFields.FullName, mappings["EmployeeName"]);
    }

    [Fact]
    public void BuildSuggestedMappings_HandlesEmptyHeaders()
    {
        var headers = Array.Empty<string>();

        var mappings = EmployeeImportCsvService.BuildSuggestedMappings(headers);

        Assert.Empty(mappings);
    }
}

public class EmployeeImportProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ImportsNewEmployees()
    {
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var dbName = nameof(ProcessAsync_ImportsNewEmployees);

        var options = CreateDbOptions(dbName);
        await SeedOrganizationAndRoleAsync(options, orgId, roleId);

        var uploadSessionId = await SeedUploadSessionAsync(options, orgId, new[]
        {
            new Dictionary<string, string?> { ["First"] = "Alice", ["Last"] = "Smith", ["Email"] = "alice@test.com", ["Phone"] = "555-0001" },
            new Dictionary<string, string?> { ["First"] = "Bob", ["Last"] = "Jones", ["Email"] = "bob@test.com", ["Phone"] = "555-0002" }
        });

        await SeedImportJobAsync(options, jobId, orgId);

        var columnMappings = new Dictionary<string, string?>
        {
            ["First"] = EmployeeImportTargetFields.FirstName,
            ["Last"] = EmployeeImportTargetFields.LastName,
            ["Email"] = EmployeeImportTargetFields.Email,
            ["Phone"] = EmployeeImportTargetFields.PhoneNumber
        };

        var processor = CreateProcessor(options);
        await processor.ProcessAsync(jobId, orgId, uploadSessionId, columnMappings);

        await using var dbContext = new JobFlowDbContext(options);
        var importJob = await dbContext.Set<EmployeeImportJob>().FirstAsync(x => x.Id == jobId);

        Assert.Equal("completed", importJob.Status);
        Assert.Equal(2, importJob.TotalRows);
        Assert.Equal(2, importJob.ProcessedRows);
        Assert.Equal(2, importJob.SucceededRows);
        Assert.Equal(0, importJob.FailedRows);

        var employees = await dbContext.Set<Employee>()
            .Where(e => e.OrganizationId == orgId)
            .ToListAsync();

        Assert.Equal(2, employees.Count);
        Assert.Contains(employees, e => e.FirstName == "Alice" && e.Email == "alice@test.com");
        Assert.Contains(employees, e => e.FirstName == "Bob" && e.Email == "bob@test.com");
        Assert.All(employees, e => Assert.Equal(roleId, e.RoleId));
    }

    [Fact]
    public async Task ProcessAsync_DeduplicatesByEmail()
    {
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var dbName = nameof(ProcessAsync_DeduplicatesByEmail);

        var options = CreateDbOptions(dbName);
        await SeedOrganizationAndRoleAsync(options, orgId, roleId);

        // Seed an existing employee
        await using (var ctx = new JobFlowDbContext(options))
        {
            ctx.Set<Employee>().Add(new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                FirstName = "Old",
                LastName = "Name",
                Email = "alice@test.com",
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        var uploadSessionId = await SeedUploadSessionAsync(options, orgId, new[]
        {
            new Dictionary<string, string?> { ["First"] = "Alice", ["Last"] = "Smith", ["Email"] = "alice@test.com" }
        });

        await SeedImportJobAsync(options, jobId, orgId);

        var columnMappings = new Dictionary<string, string?>
        {
            ["First"] = EmployeeImportTargetFields.FirstName,
            ["Last"] = EmployeeImportTargetFields.LastName,
            ["Email"] = EmployeeImportTargetFields.Email
        };

        var processor = CreateProcessor(options);
        await processor.ProcessAsync(jobId, orgId, uploadSessionId, columnMappings);

        await using var dbContext = new JobFlowDbContext(options);
        var employees = await dbContext.Set<Employee>()
            .Where(e => e.OrganizationId == orgId)
            .ToListAsync();

        // Should not create a duplicate; should update existing
        Assert.Single(employees);
        Assert.Equal("Alice", employees[0].FirstName);
        Assert.Equal("Smith", employees[0].LastName);
    }

    [Fact]
    public async Task ProcessAsync_SkipsRowsWithNoUsableData()
    {
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var dbName = nameof(ProcessAsync_SkipsRowsWithNoUsableData);

        var options = CreateDbOptions(dbName);
        await SeedOrganizationAndRoleAsync(options, orgId, roleId);

        var uploadSessionId = await SeedUploadSessionAsync(options, orgId, new[]
        {
            new Dictionary<string, string?> { ["First"] = "", ["Last"] = "", ["Email"] = "", ["Phone"] = "" },
            new Dictionary<string, string?> { ["First"] = "Valid", ["Last"] = "Person", ["Email"] = "valid@test.com" }
        });

        await SeedImportJobAsync(options, jobId, orgId);

        var columnMappings = new Dictionary<string, string?>
        {
            ["First"] = EmployeeImportTargetFields.FirstName,
            ["Last"] = EmployeeImportTargetFields.LastName,
            ["Email"] = EmployeeImportTargetFields.Email,
            ["Phone"] = EmployeeImportTargetFields.PhoneNumber
        };

        var processor = CreateProcessor(options);
        await processor.ProcessAsync(jobId, orgId, uploadSessionId, columnMappings);

        await using var dbContext = new JobFlowDbContext(options);
        var importJob = await dbContext.Set<EmployeeImportJob>().FirstAsync(x => x.Id == jobId);

        Assert.Equal("completed", importJob.Status);
        Assert.Equal(1, importJob.SucceededRows);
        Assert.Equal(1, importJob.FailedRows);

        var errors = await dbContext.Set<EmployeeImportJobError>()
            .Where(e => e.EmployeeImportJobId == jobId)
            .ToListAsync();

        Assert.Single(errors);
        Assert.Contains("no usable employee data", errors[0].Message);
    }

    [Fact]
    public async Task ProcessAsync_FailsWhenNoRolesExist()
    {
        var orgId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var dbName = nameof(ProcessAsync_FailsWhenNoRolesExist);

        var options = CreateDbOptions(dbName);

        // Seed org without a role
        await using (var ctx = new JobFlowDbContext(options))
        {
            ctx.Set<Organization>().Add(new Organization
            {
                Id = orgId,
                OrganizationTypeId = Guid.NewGuid(),
                OrganizationName = "No Role Org",
                IsActive = true
            });
            await ctx.SaveChangesAsync();
        }

        var uploadSessionId = await SeedUploadSessionAsync(options, orgId, new[]
        {
            new Dictionary<string, string?> { ["First"] = "Alice", ["Last"] = "Smith" }
        });

        await SeedImportJobAsync(options, jobId, orgId);

        var columnMappings = new Dictionary<string, string?>
        {
            ["First"] = EmployeeImportTargetFields.FirstName,
            ["Last"] = EmployeeImportTargetFields.LastName
        };

        var processor = CreateProcessor(options);
        await processor.ProcessAsync(jobId, orgId, uploadSessionId, columnMappings);

        await using var dbContext = new JobFlowDbContext(options);
        var importJob = await dbContext.Set<EmployeeImportJob>().FirstAsync(x => x.Id == jobId);

        Assert.Equal("failed", importJob.Status);
        Assert.Contains("No employee roles exist", importJob.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_RequiresFirstName()
    {
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var dbName = nameof(ProcessAsync_RequiresFirstName);

        var options = CreateDbOptions(dbName);
        await SeedOrganizationAndRoleAsync(options, orgId, roleId);

        var uploadSessionId = await SeedUploadSessionAsync(options, orgId, new[]
        {
            new Dictionary<string, string?> { ["Email"] = "nofirst@test.com", ["Phone"] = "555-0001" }
        });

        await SeedImportJobAsync(options, jobId, orgId);

        var columnMappings = new Dictionary<string, string?>
        {
            ["Email"] = EmployeeImportTargetFields.Email,
            ["Phone"] = EmployeeImportTargetFields.PhoneNumber
        };

        var processor = CreateProcessor(options);
        await processor.ProcessAsync(jobId, orgId, uploadSessionId, columnMappings);

        await using var dbContext = new JobFlowDbContext(options);
        var importJob = await dbContext.Set<EmployeeImportJob>().FirstAsync(x => x.Id == jobId);

        Assert.Equal("completed", importJob.Status);
        Assert.Equal(0, importJob.SucceededRows);
        Assert.Equal(1, importJob.FailedRows);

        var errors = await dbContext.Set<EmployeeImportJobError>()
            .Where(e => e.EmployeeImportJobId == jobId)
            .ToListAsync();

        Assert.Single(errors);
        Assert.Contains("First name is required", errors[0].Message);
    }

    // --- Helpers ---

    private static DbContextOptions<JobFlowDbContext> CreateDbOptions(string dbName)
    {
        return new DbContextOptionsBuilder<JobFlowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    private static async Task SeedOrganizationAndRoleAsync(DbContextOptions<JobFlowDbContext> options, Guid orgId, Guid roleId)
    {
        await using var ctx = new JobFlowDbContext(options);
        ctx.Set<Organization>().Add(new Organization
        {
            Id = orgId,
            OrganizationTypeId = Guid.NewGuid(),
            OrganizationName = "Test Org",
            IsActive = true
        });
        ctx.Set<EmployeeRole>().Add(new EmployeeRole
        {
            Id = roleId,
            OrganizationId = orgId,
            Name = "TECHNICIAN",
            Description = "Default role"
        });
        await ctx.SaveChangesAsync();
    }

    private static async Task<Guid> SeedUploadSessionAsync(
        DbContextOptions<JobFlowDbContext> options,
        Guid orgId,
        IReadOnlyList<Dictionary<string, string?>> rows)
    {
        await using var ctx = new JobFlowDbContext(options);
        var sessionId = Guid.NewGuid();
        ctx.Set<EmployeeImportUploadSession>().Add(new EmployeeImportUploadSession
        {
            Id = sessionId,
            OrganizationId = orgId,
            SourceSystem = "csv",
            Status = "active",
            TotalRows = rows.Count,
            CreatedAt = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        });

        for (var i = 0; i < rows.Count; i++)
        {
            ctx.Set<EmployeeImportUploadRow>().Add(new EmployeeImportUploadRow
            {
                Id = Guid.NewGuid(),
                EmployeeImportUploadSessionId = sessionId,
                RowNumber = i + 2,
                RowDataJson = System.Text.Json.JsonSerializer.Serialize(rows[i]),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await ctx.SaveChangesAsync();
        return sessionId;
    }

    private static async Task SeedImportJobAsync(DbContextOptions<JobFlowDbContext> options, Guid jobId, Guid orgId)
    {
        await using var ctx = new JobFlowDbContext(options);
        ctx.Set<EmployeeImportJob>().Add(new EmployeeImportJob
        {
            Id = jobId,
            OrganizationId = orgId,
            SourceSystem = "csv",
            Status = "queued",
            TotalRows = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
        await ctx.SaveChangesAsync();
    }

    private static EmployeeImportProcessor CreateProcessor(DbContextOptions<JobFlowDbContext> options)
    {
        var factory = new TestDbContextFactory(options);
        var uploadSessionService = new EmployeeImportUploadSessionService(factory);
        return new EmployeeImportProcessor(factory, uploadSessionService, NullLogger<EmployeeImportProcessor>.Instance);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<JobFlowDbContext>
    {
        private readonly DbContextOptions<JobFlowDbContext> _options;

        public TestDbContextFactory(DbContextOptions<JobFlowDbContext> options)
        {
            _options = options;
        }

        public JobFlowDbContext CreateDbContext()
        {
            return new JobFlowDbContext(_options);
        }
    }
}
