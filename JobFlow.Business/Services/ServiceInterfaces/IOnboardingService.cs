using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOnboardingService
    {
        Task<Result<IEnumerable<OrganizationOnboardingStep>>> GetStepsAsync(Guid organizationId);
        Task<Result<OrganizationOnboardingStep>> MarkStepCompleteAsync(Guid organizationId, string stepName);
    }
}
