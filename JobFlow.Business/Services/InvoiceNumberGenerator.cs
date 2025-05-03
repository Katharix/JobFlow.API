using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class InvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<InvoiceSequence> _sequenceRepo;
        private readonly ILogger<InvoiceNumberGenerator> _logger;

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
            int nextSeq;

            // Acquire a serializable transaction to prevent race conditions
            var dbContext = (DbContext)_unitOfWork;
            using (var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
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
                nextSeq = sequence.LastSequence;

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            var invoiceNumber = $"{year}-{nextSeq:D4}";
            _logger.LogInformation(
                "Generated invoice number {InvoiceNumber} for organization {OrgId}",
                invoiceNumber, organizationId);

            return invoiceNumber;
        }
    }
}
