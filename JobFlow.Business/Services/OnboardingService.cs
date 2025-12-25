using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class OnboardingService : IOnboardingService
{
    private readonly IRepository<Organization> orgRepo;
    private readonly IRepository<OrganizationOnboardingStep> stepRepo;
    private readonly IUnitOfWork uow;

    public OnboardingService(IUnitOfWork uow)
    {
        this.uow = uow;
        orgRepo = uow.RepositoryOf<Organization>();
        stepRepo = uow.RepositoryOf<OrganizationOnboardingStep>();
    }

    public async Task<Result<IEnumerable<OnboardingStepDto>>> GetChecklistAsync(Guid orgId)
    {
        var org = await orgRepo.GetByIdAsync(orgId);
        if (org == null)
            return Result.Failure<IEnumerable<OnboardingStepDto>>(OnboardingErrors.OrganizationNotFound);

        var progress = await stepRepo.Query()
            .Where(x => x.OrganizationId == orgId)
            .ToListAsync();

        var steps = OnboardingCatalog.ApplicableSteps(org)
            .Select(def =>
            {
                var row = progress.FirstOrDefault(p => p.StepName == def.Key);
                return new OnboardingStepDto
                {
                    Key = def.Key,
                    Title = def.Title,
                    Order = def.Order,
                    IsCompleted = row?.IsCompleted ?? false,
                    CompletedAt = row?.CompletedAt
                };
            });

        return Result.Success(steps);
    }

    public async Task<Result> MarkStepCompleteAsync(Guid orgId, string stepKey)
    {
        if (!OnboardingCatalog.IsKnown(stepKey))
            return Result.Failure(OnboardingErrors.UnknownStep(stepKey));

        var step = await stepRepo.Query()
            .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.StepName == stepKey);

        if (step == null)
        {
            step = new OrganizationOnboardingStep
            {
                OrganizationId = orgId,
                StepName = stepKey,
                IsCompleted = true,
                CompletedAt = DateTimeOffset.UtcNow
            };
            await stepRepo.AddAsync(step);
        }
        else if (!step.IsCompleted)
        {
            step.IsCompleted = true;
            step.CompletedAt = DateTimeOffset.UtcNow;
            stepRepo.Update(step);
        }

        await uow.SaveChangesAsync();
        return Result.Success();
    }
}