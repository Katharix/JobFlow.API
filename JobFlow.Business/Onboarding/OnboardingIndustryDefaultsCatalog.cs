namespace JobFlow.Business.Onboarding;

public record IndustryServiceSeed(
    string Name,
    string Description,
    string Unit,
    decimal Price);

public record IndustryDefaults(
    string OrgTypeName,
    string TemplateSuggestionName,
    int PaymentTermsDays,
    IReadOnlyList<IndustryServiceSeed> Services);

public static class OnboardingIndustryDefaultsCatalog
{
    public static readonly IReadOnlyList<IndustryDefaults> Catalog =
    [
        new IndustryDefaults(
            "Landscaping",
            "Landscaping Estimate",
            15,
            [
                new IndustryServiceSeed("Lawn mowing", "Standard residential lawn cut.", "cut", 85m),
                new IndustryServiceSeed("Hedge trimming", "Trim and shape shrubs and hedges.", "visit", 120m),
                new IndustryServiceSeed("Leaf cleanup", "Seasonal leaf removal and disposal.", "visit", 150m),
                new IndustryServiceSeed("Irrigation check", "Inspection and adjustment of irrigation system.", "visit", 95m),
                new IndustryServiceSeed("Mulch install", "Supply and spread mulch.", "yard", 65m),
                new IndustryServiceSeed("Landscape design consult", "On-site design consultation.", "hour", 125m)
            ]
        ),
        new IndustryDefaults(
            "Cleaning",
            "Cleaning Service Estimate",
            0,
            [
                new IndustryServiceSeed("Standard house cleaning", "Routine cleaning of all rooms.", "visit", 150m),
                new IndustryServiceSeed("Deep clean", "Thorough top-to-bottom cleaning.", "visit", 250m),
                new IndustryServiceSeed("Move-in/move-out clean", "Full property clean for move-in or move-out.", "job", 350m),
                new IndustryServiceSeed("Office cleaning", "Commercial office space cleaning.", "visit", 200m),
                new IndustryServiceSeed("Window cleaning", "Interior and exterior window washing.", "visit", 120m),
                new IndustryServiceSeed("Carpet cleaning", "Steam or dry carpet cleaning per room.", "room", 75m)
            ]
        ),
        new IndustryDefaults(
            "IT",
            "IT Services Estimate",
            30,
            [
                new IndustryServiceSeed("Remote support", "Remote troubleshooting and repair.", "hour", 120m),
                new IndustryServiceSeed("On-site visit", "Technician on-site support.", "visit", 175m),
                new IndustryServiceSeed("Network setup", "Design and install LAN/WiFi infrastructure.", "job", 450m),
                new IndustryServiceSeed("Workstation setup", "Configure and provision a workstation.", "unit", 250m),
                new IndustryServiceSeed("Managed services", "Monthly monitoring and maintenance plan.", "month", 299m),
                new IndustryServiceSeed("Data backup setup", "Configure cloud or local backup solution.", "job", 350m)
            ]
        ),
        new IndustryDefaults(
            "HVAC",
            "HVAC Estimate",
            15,
            [
                new IndustryServiceSeed("Diagnostic visit", "On-site diagnosis and troubleshooting.", "visit", 95m),
                new IndustryServiceSeed("AC tune-up", "Seasonal air conditioning service.", "unit", 149m),
                new IndustryServiceSeed("Furnace inspection", "Annual furnace safety inspection.", "unit", 129m),
                new IndustryServiceSeed("Filter replacement", "Supply and install replacement filter.", "unit", 45m),
                new IndustryServiceSeed("Refrigerant recharge", "Recharge AC refrigerant to spec.", "unit", 250m),
                new IndustryServiceSeed("System installation", "Full HVAC system install.", "job", 2500m)
            ]
        ),
        new IndustryDefaults(
            "General",
            "General Services Estimate",
            30,
            [
                new IndustryServiceSeed("Service call", "Initial on-site service visit.", "visit", 89m),
                new IndustryServiceSeed("Labor", "General field labor.", "hour", 95m),
                new IndustryServiceSeed("Materials", "Estimated materials and supplies.", "lot", 250m),
                new IndustryServiceSeed("Project consultation", "Planning and scoping consultation.", "hour", 75m),
                new IndustryServiceSeed("Standard repair", "Most common repair or fix.", "job", 200m)
            ]
        )
    ];

    public static IndustryDefaults? TryGetByOrgTypeName(string? orgTypeName)
    {
        if (string.IsNullOrWhiteSpace(orgTypeName))
            return null;

        return Catalog.FirstOrDefault(c =>
            c.OrgTypeName.Equals(orgTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static IndustryDefaults GetGeneralDefaults()
    {
        return Catalog.First(c => c.OrgTypeName == "General");
    }
}
