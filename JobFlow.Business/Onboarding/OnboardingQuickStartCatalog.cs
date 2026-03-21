using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Onboarding;

public record OnboardingQuickStartTrackDefinition(
    string Key,
    string Title,
    string Description);

public record OnboardingQuickStartPresetDefinition(
    string Key,
    string Title,
    string Description,
    IReadOnlyList<OnboardingQuickStartServiceSeed> DefaultServices,
    IReadOnlyList<WorkflowStatusSeed> SuggestedStatuses);

public record OnboardingQuickStartServiceSeed(
    string Name,
    string Description,
    string Unit,
    decimal Price);

public record WorkflowStatusSeed(
    string StatusKey,
    string Label,
    int SortOrder);

public static class OnboardingQuickStartCatalog
{
    public static readonly IReadOnlyList<OnboardingQuickStartTrackDefinition> Tracks =
    [
        new(
            OnboardingTrackKeys.GetPaidFast,
            "Get paid fast",
            "Prioritize invoices and payments so you can collect revenue quickly."
        ),
        new(
            OnboardingTrackKeys.GetOrganizedFirst,
            "Get organized first",
            "Set up customers, jobs, and schedules before you focus on billing."
        )
    ];

    public static readonly IReadOnlyList<OnboardingQuickStartPresetDefinition> Presets =
    [
        new(
            OnboardingPresetKeys.HomeServices,
            "Home services",
            "HVAC, plumbing, electrical, cleaning, and recurring maintenance teams.",
            [
                new OnboardingQuickStartServiceSeed(
                    "Diagnostic visit",
                    "On-site evaluation and troubleshooting.",
                    "visit",
                    89m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Standard repair",
                    "Most common repair or fix.",
                    "job",
                    250m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Maintenance plan",
                    "Monthly service agreement.",
                    "month",
                    49m
                )
            ],
            [
                new WorkflowStatusSeed("Draft", "New Request", 0),
                new WorkflowStatusSeed("Approved", "Booked", 1),
                new WorkflowStatusSeed("InProgress", "In Service", 2),
                new WorkflowStatusSeed("Completed", "Wrapped", 3),
                new WorkflowStatusSeed("Cancelled", "Cancelled", 4),
                new WorkflowStatusSeed("Failed", "Reschedule", 5)
            ]
        ),
        new(
            OnboardingPresetKeys.Construction,
            "Construction / contracting",
            "General contracting, remodels, site work, and specialty trades.",
            [
                new OnboardingQuickStartServiceSeed(
                    "Site walkthrough",
                    "On-site scope review and measurements.",
                    "visit",
                    150m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Labor - crew",
                    "Field labor billed hourly.",
                    "hour",
                    125m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Materials allowance",
                    "Estimated materials and supplies.",
                    "lot",
                    500m
                )
            ],
            [
                new WorkflowStatusSeed("Draft", "Lead", 0),
                new WorkflowStatusSeed("Approved", "Estimate Approved", 1),
                new WorkflowStatusSeed("InProgress", "In Progress", 2),
                new WorkflowStatusSeed("Completed", "Final Walkthrough", 3),
                new WorkflowStatusSeed("Cancelled", "Cancelled", 4),
                new WorkflowStatusSeed("Failed", "Blocked", 5)
            ]
        ),
        new(
            OnboardingPresetKeys.CreativeDesign,
            "Creative / design",
            "Branding, design studios, production, and creative agencies.",
            [
                new OnboardingQuickStartServiceSeed(
                    "Discovery session",
                    "Initial briefing and creative alignment.",
                    "hour",
                    150m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Design concept",
                    "Concept development and visuals.",
                    "package",
                    800m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Production sprint",
                    "Execution and revisions.",
                    "hour",
                    120m
                )
            ],
            [
                new WorkflowStatusSeed("Draft", "Intake", 0),
                new WorkflowStatusSeed("Approved", "Concept Approved", 1),
                new WorkflowStatusSeed("InProgress", "Production", 2),
                new WorkflowStatusSeed("Completed", "Delivered", 3),
                new WorkflowStatusSeed("Cancelled", "Cancelled", 4),
                new WorkflowStatusSeed("Failed", "Paused", 5)
            ]
        ),
        new(
            OnboardingPresetKeys.TechRepair,
            "Tech repair",
            "Device repair, IT support, and on-site troubleshooting.",
            [
                new OnboardingQuickStartServiceSeed(
                    "Device diagnostics",
                    "Hardware and software inspection.",
                    "device",
                    65m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Repair labor",
                    "Repair time and skill.",
                    "hour",
                    110m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Parts replacement",
                    "Common replacement parts.",
                    "part",
                    75m
                )
            ],
            [
                new WorkflowStatusSeed("Draft", "Intake", 0),
                new WorkflowStatusSeed("Approved", "Authorized", 1),
                new WorkflowStatusSeed("InProgress", "In Repair", 2),
                new WorkflowStatusSeed("Completed", "Ready for Pickup", 3),
                new WorkflowStatusSeed("Cancelled", "Cancelled", 4),
                new WorkflowStatusSeed("Failed", "Unrepairable", 5)
            ]
        ),
        new(
            OnboardingPresetKeys.Consulting,
            "Consulting",
            "Consultants, coaching, and professional service providers.",
            [
                new OnboardingQuickStartServiceSeed(
                    "Strategy call",
                    "Initial call and planning.",
                    "hour",
                    200m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Monthly retainer",
                    "Ongoing advisory support.",
                    "month",
                    1200m
                ),
                new OnboardingQuickStartServiceSeed(
                    "Workshop",
                    "Facilitated working session.",
                    "session",
                    950m
                )
            ],
            [
                new WorkflowStatusSeed("Draft", "Inquiry", 0),
                new WorkflowStatusSeed("Approved", "Engaged", 1),
                new WorkflowStatusSeed("InProgress", "In Session", 2),
                new WorkflowStatusSeed("Completed", "Delivered", 3),
                new WorkflowStatusSeed("Cancelled", "Cancelled", 4),
                new WorkflowStatusSeed("Failed", "On Hold", 5)
            ]
        )
    ];

    public static OnboardingQuickStartTrackDefinition GetTrackOrDefault(string? key)
    {
        var normalized = OnboardingTrackKeys.Normalize(key);
        return Tracks.FirstOrDefault(t => t.Key == normalized)
            ?? Tracks.First(t => t.Key == OnboardingTrackKeys.GetPaidFast);
    }

    public static bool IsKnownTrack(string? key)
    {
        var normalized = OnboardingTrackKeys.Normalize(key);
        return Tracks.Any(t => t.Key == normalized);
    }

    public static bool IsKnownPreset(string? key)
    {
        var normalized = OnboardingPresetKeys.Normalize(key);
        return Presets.Any(p => p.Key == normalized);
    }

    public static OnboardingQuickStartPresetDefinition? TryGetPreset(string? key)
    {
        var normalized = OnboardingPresetKeys.Normalize(key);
        return Presets.FirstOrDefault(p => p.Key == normalized);
    }

    public static List<OnboardingQuickStartTrackDto> BuildTrackDtos()
    {
        return Tracks
            .Select(track => new OnboardingQuickStartTrackDto
            {
                Key = track.Key,
                Title = track.Title,
                Description = track.Description
            })
            .ToList();
    }

    public static List<OnboardingQuickStartPresetDto> BuildPresetDtos()
    {
        return Presets
            .Select(preset => new OnboardingQuickStartPresetDto
            {
                Key = preset.Key,
                Title = preset.Title,
                Description = preset.Description,
                DefaultServices = preset.DefaultServices
                    .Select(service => new OnboardingQuickStartServiceDto
                    {
                        Name = service.Name,
                        Description = service.Description,
                        Unit = service.Unit,
                        Price = service.Price
                    })
                    .ToList()
            })
            .ToList();
    }
}
