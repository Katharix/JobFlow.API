namespace JobFlow.Business.Models.DTOs;

public class OnboardingQuickStartTrackDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class OnboardingQuickStartServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class OnboardingQuickStartPresetDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<OnboardingQuickStartServiceDto> DefaultServices { get; set; } = new();
}

public class OnboardingQuickStartStateDto
{
    public string? SelectedTrackKey { get; set; }
    public string? SelectedPresetKey { get; set; }
    public bool IsPresetApplied { get; set; }
    public List<OnboardingQuickStartTrackDto> Tracks { get; set; } = new();
    public List<OnboardingQuickStartPresetDto> Presets { get; set; } = new();
}

public class OnboardingQuickStartApplyRequestDto
{
    public string TrackKey { get; set; } = string.Empty;
    public string PresetKey { get; set; } = string.Empty;
}
