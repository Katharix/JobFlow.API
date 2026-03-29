using System.IO.Compression;
using System.Text;
using System.Text.Json;
using JobFlow.API.Models;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Services;

public sealed class DataExportBuilderService
{
    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;

    public DataExportBuilderService(IDbContextFactory<JobFlowDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<(byte[] Content, string FileName)> BuildJsonBundleAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var export = await BuildExportBundleAsync(organizationId, cancellationToken);
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"jobflow-data-export-{organizationId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        return (bytes, fileName);
    }

    public async Task<(byte[] Content, string FileName)> BuildClientsCsvAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var clients = await dbContext.Set<OrganizationClient>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,FirstName,LastName,EmailAddress,PhoneNumber,Address1,Address2,City,State,ZipCode");

        foreach (var c in clients)
        {
            sb.AppendLine(string.Join(',',
                Csv(c.Id.ToString()),
                Csv(c.FirstName),
                Csv(c.LastName),
                Csv(c.EmailAddress),
                Csv(c.PhoneNumber),
                Csv(c.Address1),
                Csv(c.Address2),
                Csv(c.City),
                Csv(c.State),
                Csv(c.ZipCode)));
        }

        var fileName = $"jobflow-clients-{organizationId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return (Encoding.UTF8.GetBytes(sb.ToString()), fileName);
    }

    public async Task<(byte[] Content, string FileName)> BuildZipPackageAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var (jsonContent, jsonFileName) = await BuildJsonBundleAsync(organizationId, cancellationToken);
        var (csvContent, csvFileName) = await BuildClientsCsvAsync(organizationId, cancellationToken);

        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var jsonEntry = archive.CreateEntry(jsonFileName, CompressionLevel.Fastest);
            await using (var jsonEntryStream = jsonEntry.Open())
            {
                await jsonEntryStream.WriteAsync(jsonContent, cancellationToken);
            }

            var csvEntry = archive.CreateEntry(csvFileName, CompressionLevel.Fastest);
            await using (var csvEntryStream = csvEntry.Open())
            {
                await csvEntryStream.WriteAsync(csvContent, cancellationToken);
            }

            var readmeEntry = archive.CreateEntry("README.txt", CompressionLevel.Fastest);
            await using var readmeStream = readmeEntry.Open();
            await using var writer = new StreamWriter(readmeStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(
                "JobFlow Data Export\n" +
                $"Generated at (UTC): {DateTime.UtcNow:O}\n" +
                "Contents:\n" +
                $"- {jsonFileName} (full organization data bundle)\n" +
                $"- {csvFileName} (clients only)\n");
            await writer.FlushAsync(cancellationToken);
        }

        var zipName = $"jobflow-export-{organizationId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        return (stream.ToArray(), zipName);
    }

    private async Task<DataExportBundleDto> BuildExportBundleAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var clients = await dbContext.Set<OrganizationClient>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new DataExportClientDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber,
                Address1 = x.Address1,
                Address2 = x.Address2,
                City = x.City,
                State = x.State,
                ZipCode = x.ZipCode
            })
            .ToListAsync(cancellationToken);

        var jobs = await dbContext.Set<Job>()
            .AsNoTracking()
            .Where(x => x.OrganizationClient.OrganizationId == organizationId)
            .Select(x => new DataExportJobDto
            {
                Id = x.Id,
                OrganizationClientId = x.OrganizationClientId,
                Title = x.Title,
                Comments = x.Comments,
                LifecycleStatus = x.LifecycleStatus.ToString(),
                InvoicingWorkflow = x.InvoicingWorkflow.HasValue ? x.InvoicingWorkflow.Value.ToString() : null
            })
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new DataExportInvoiceDto
            {
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                OrganizationClientId = x.OrganizationClientId,
                JobId = x.JobId,
                InvoiceDate = x.InvoiceDate,
                DueDate = x.DueDate,
                TotalAmount = x.TotalAmount,
                AmountPaid = x.AmountPaid,
                BalanceDue = x.TotalAmount - x.AmountPaid,
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        var employees = await dbContext.Set<Employee>()
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new DataExportEmployeeDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                RoleName = x.Role.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return new DataExportBundleDto
        {
            ExportedAtUtc = DateTime.UtcNow.ToString("O"),
            OrganizationId = organizationId,
            Clients = clients,
            Jobs = jobs,
            Invoices = invoices,
            Employees = employees
        };
    }

    private static string Csv(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}