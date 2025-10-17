using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class OnboardingService : IOnboardingService
    {
        private readonly ILogger<OnboardingService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<OrganizationOnboardingStep> stepsRepo;
        private readonly IRepository<Organization> orgRepo;

        public OnboardingService(ILogger<OnboardingService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.stepsRepo = unitOfWork.RepositoryOf<OrganizationOnboardingStep>();
            this.orgRepo = unitOfWork.RepositoryOf<Organization>();
        }

        public async Task<Result<IEnumerable<OrganizationOnboardingStep>>> GetStepsAsync(Guid organizationId)
        {
            var steps = await stepsRepo.Query()
                .Where(s => s.OrganizationId == organizationId)
                .OrderBy(s => s.StepName)
                .ToListAsync();

            return Result<IEnumerable<OrganizationOnboardingStep>>.Success(steps.AsEnumerable());
        }

        public async Task<Result<OrganizationOnboardingStep>> MarkStepCompleteAsync(Guid organizationId, string stepName)
        {
            var step = await stepsRepo.Query()
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.StepName == stepName);

            if (step == null)
            {
                step = new OrganizationOnboardingStep
                {
                    OrganizationId = organizationId,
                    StepName = stepName,
                    IsCompleted = true,
                    CompletedAt = DateTimeOffset.UtcNow
                };
                await stepsRepo.AddAsync(step);
            }
            else
            {
                step.IsCompleted = true;
                step.CompletedAt = DateTimeOffset.UtcNow;
                stepsRepo.Update(step);
            }

            // If all required steps done → flag org complete
            var required = await stepsRepo.Query()
                .Where(s => s.OrganizationId == organizationId)
                .ToListAsync();

            var org = await orgRepo.GetByIdAsync(organizationId);
            org.OnBoardingComplete = required.All(x => x.IsCompleted);
            org.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();
            return Result<OrganizationOnboardingStep>.Success(step);
        }
    }
}
