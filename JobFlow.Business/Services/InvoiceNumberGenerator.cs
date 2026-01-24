using System.Data;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly ILogger<InvoiceNumberGenerator> _logger;
    private readonly IRepository<InvoiceSequence> _sequenceRepo;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceNumberGenerator(
        IUnitOfWork unitOfWork,
        ILogger<InvoiceNumberGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _sequenceRepo = _unitOfWork.RepositoryOf<InvoiceSequence>();
        _logger = logger;
    }

    public async Task<string> GenerateAsync(Guid organizationId)
    {
        var year = DateTime.UtcNow.Year;
        var invoiceNumber = string.Empty;

        var dbContext = _unitOfWork.Context;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var sequence = await _sequenceRepo.Query()
                .SingleOrDefaultAsync(s => s.OrganizationId == organizationId && s.Year == year);

            if (sequence == null)
            {
                sequence = new InvoiceSequence
                {
                    OrganizationId = organizationId,
                    Year = year,
                    LastSequence = 0
                };
                await _sequenceRepo.AddAsync(sequence);
            }

            sequence.LastSequence++;
            var nextSeq = sequence.LastSequence;

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            invoiceNumber = $"{year}-{nextSeq:D4}";
        });

        _logger.LogInformation(
            "Generated invoice number {InvoiceNumber} for organization {OrgId}",
            invoiceNumber, organizationId);

        return invoiceNumber;
    }
}