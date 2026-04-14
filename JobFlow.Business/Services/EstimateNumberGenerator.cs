using System.Data;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class EstimateNumberGenerator : IEstimateNumberGenerator
{
    private readonly ILogger<EstimateNumberGenerator> _logger;
    private readonly IRepository<EstimateSequence> _sequenceRepo;
    private readonly IUnitOfWork _unitOfWork;

    public EstimateNumberGenerator(
        IUnitOfWork unitOfWork,
        ILogger<EstimateNumberGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _sequenceRepo = _unitOfWork.RepositoryOf<EstimateSequence>();
        _logger = logger;
    }

    public async Task<string> GenerateAsync(Guid organizationId)
    {
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;
        var day = now.Day;
        var estimateNumber = string.Empty;

        var dbContext = _unitOfWork.Context;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var sequence = await _sequenceRepo.Query()
                .SingleOrDefaultAsync(s =>
                    s.OrganizationId == organizationId &&
                    s.Year == year &&
                    s.Month == month &&
                    s.Day == day);

            if (sequence == null)
            {
                sequence = new EstimateSequence
                {
                    OrganizationId = organizationId,
                    Year = year,
                    Month = month,
                    Day = day,
                    LastSequence = 0
                };
                await _sequenceRepo.AddAsync(sequence);
            }

            sequence.LastSequence++;
            var nextSeq = sequence.LastSequence;

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            estimateNumber = $"EST-{year}{month:D2}{day:D2}-{nextSeq:D4}";
        });

        _logger.LogInformation(
            "Generated estimate number {EstimateNumber} for organization {OrgId}",
            estimateNumber, organizationId);

        return estimateNumber;
    }
}
