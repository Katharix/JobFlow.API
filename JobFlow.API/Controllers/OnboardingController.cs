using JobFlow.API.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [ApiController]
    [Route("api/onboarding")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            this.onboardingService = onboardingService;
        }

        [HttpGet("{organizationId}")]
        public async Task<IActionResult> GetSteps(Guid organizationId)
        {
            var result = await onboardingService.GetStepsAsync(organizationId);
            if (!result.IsSuccess) return BadRequest(result.Error);

            var dto = result.Value.Select(s => new OnboardingStepDto
            {
                Id = s.Id,
                StepName = s.StepName,
                IsCompleted = s.IsCompleted,
                CompletedAt = s.CompletedAt
            });

            return Ok(dto);
        }

        [HttpPut("{organizationId}/complete")]
        public async Task<IActionResult> MarkStepComplete(Guid organizationId, [FromBody] MarkStepRequestDto body)
        {
            var result = await onboardingService.MarkStepCompleteAsync(organizationId, body.StepName);
            if (!result.IsSuccess) return BadRequest(result.Error);

            var dto = new OnboardingStepDto
            {
                Id = result.Value.Id,
                StepName = result.Value.StepName,
                IsCompleted = result.Value.IsCompleted,
                CompletedAt = result.Value.CompletedAt
            };

            return Ok(dto);
        }
    }
}
