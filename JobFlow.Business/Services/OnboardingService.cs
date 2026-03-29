using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class OnboardingService : IOnboardingService
{
    private readonly IRepository<Organization> orgRepo;
    private readonly IRepository<PriceBookItem> priceBookItems;
    private readonly IRepository<OrganizationOnboardingStep> stepRepo;
    private readonly IWorkflowSettingsService workflowSettings;
    private readonly IUnitOfWork uow;

    public OnboardingService(IUnitOfWork uow, IWorkflowSettingsService workflowSettings)
    {
        this.uow = uow;
        this.workflowSettings = workflowSettings;
        orgRepo = uow.RepositoryOf<Organization>();
        stepRepo = uow.RepositoryOf<OrganizationOnboardingStep>();
        priceBookItems = uow.RepositoryOf<PriceBookItem>();
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

    public async Task<Result<bool>> MarkOrganizationCompleteIfEligibleAsync(Guid organizationId)
    {
        var org = await orgRepo.GetByIdAsync(organizationId);
        if (org == null)
            return Result.Failure<bool>(OnboardingErrors.OrganizationNotFound);

        var progress = await stepRepo.Query()
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync();

        var applicableSteps = OnboardingCatalog.ApplicableSteps(org).ToList();
        if (applicableSteps.Count == 0)
            return Result.Success(false);

        var allCompleted = applicableSteps.All(step =>
            progress.Any(p => p.StepName == step.Key && p.IsCompleted));

        if (!allCompleted)
            return Result.Success(false);

        if (!org.OnBoardingComplete)
        {
            org.OnBoardingComplete = true;
            await uow.SaveChangesAsync();
        }

        return Result.Success(true);
    }

    public async Task<Result<OnboardingQuickStartStateDto>> GetQuickStartStateAsync(Guid organizationId)
    {
        var org = await orgRepo.GetByIdAsync(organizationId);
        if (org == null)
            return Result.Failure<OnboardingQuickStartStateDto>(OnboardingErrors.OrganizationNotFound);

        var state = new OnboardingQuickStartStateDto
        {
            SelectedTrackKey = org.OnboardingTrack,
            SelectedPresetKey = org.OnboardingPresetKey,
            IsPresetApplied = org.OnboardingPresetAppliedAt.HasValue,
            Tracks = OnboardingQuickStartCatalog.BuildTrackDtos(),
            Presets = OnboardingQuickStartCatalog.BuildPresetDtos()
        };

        return Result.Success(state);
    }

    public async Task<Result<OnboardingQuickStartStateDto>> ApplyQuickStartAsync(
        Guid organizationId,
        OnboardingQuickStartApplyRequestDto request)
    {
        if (request == null)
        {
            return Result.Failure<OnboardingQuickStartStateDto>(
                Error.Validation("Onboarding.QuickStart.Invalid", "Quick-start selection is required."));
        }

        var normalizedTrack = OnboardingTrackKeys.Normalize(request.TrackKey);
        var normalizedPreset = OnboardingPresetKeys.Normalize(request.PresetKey);

        if (!OnboardingQuickStartCatalog.IsKnownTrack(normalizedTrack))
        {
            return Result.Failure<OnboardingQuickStartStateDto>(
                Error.Validation("Onboarding.QuickStart.Track", "Unknown onboarding track."));
        }

        if (!OnboardingQuickStartCatalog.IsKnownPreset(normalizedPreset))
        {
            return Result.Failure<OnboardingQuickStartStateDto>(
                Error.Validation("Onboarding.QuickStart.Preset", "Unknown industry preset."));
        }

        var org = await orgRepo.GetByIdAsync(organizationId);
        if (org == null)
            return Result.Failure<OnboardingQuickStartStateDto>(OnboardingErrors.OrganizationNotFound);

        var preset = OnboardingQuickStartCatalog.TryGetPreset(normalizedPreset);
        if (preset == null)
        {
            return Result.Failure<OnboardingQuickStartStateDto>(
                Error.Validation("Onboarding.QuickStart.Preset", "Unknown industry preset."));
        }

        org.OnboardingTrack = normalizedTrack;
        org.OnboardingTrackSelectedAt ??= DateTimeOffset.UtcNow;
        org.OnboardingPresetKey = normalizedPreset;
        org.OnboardingPresetAppliedAt = DateTimeOffset.UtcNow;

        await uow.SaveChangesAsync();

        await SeedPriceBookAsync(organizationId, preset);

        var statusRequest = preset.SuggestedStatuses
            .OrderBy(s => s.SortOrder)
            .Select(s => new WorkflowStatusUpsertRequestDto
            {
                StatusKey = s.StatusKey,
                Label = s.Label,
                SortOrder = s.SortOrder
            })
            .ToList();

        var statusResult = await workflowSettings.UpsertJobLifecycleStatusesAsync(
            organizationId,
            statusRequest);

        if (statusResult.IsFailure)
        {
            return Result.Failure<OnboardingQuickStartStateDto>(statusResult.Error);
        }

        await MarkStepCompleteAsync(organizationId, OnboardingStepKeys.ChooseTrack);
        await MarkStepCompleteAsync(organizationId, OnboardingStepKeys.ChooseIndustryPreset);

        return await GetQuickStartStateAsync(organizationId);
    }

    private async Task SeedPriceBookAsync(Guid organizationId, OnboardingQuickStartPresetDefinition preset)
    {
        var existingNames = await priceBookItems.Query()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var service in preset.DefaultServices)
        {
            if (existingNames.Any(name =>
                    string.Equals(name, service.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            await priceBookItems.AddAsync(new PriceBookItem
            {
                OrganizationId = organizationId,
                Name = service.Name,
                Description = service.Description,
                Unit = service.Unit,
                Price = service.Price,
                Cost = 0m,
                PricePerUnit = service.Price,
                ItemType = PriceBookItemType.Service
            });
        }

        await uow.SaveChangesAsync();
    }
}