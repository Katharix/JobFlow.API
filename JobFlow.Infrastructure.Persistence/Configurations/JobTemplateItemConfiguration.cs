using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class JobTemplateItemConfiguration : IEntityTypeConfiguration<JobTemplateItem>
{
    public void Configure(EntityTypeBuilder<JobTemplateItem> builder)
    {
        builder.ToTable("JobTemplateItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(i => i.Description)
            .HasMaxLength(240);
        builder.HasOne(i => i.Template)
            .WithMany(t => t.Items)
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(i => i.IsActive);

        var c = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            // ── General Contracting > Home renovation ─────
            Item("4a4b4c4d-0101-0101-0101-010101010101", "3a3b3c3d-0101-0101-0101-010101010101", "Site assessment", "Walk property and scope of work.", 1, c),
            Item("4a4b4c4d-0101-0101-0101-010101010102", "3a3b3c3d-0101-0101-0101-010101010101", "Demo & prep", "Remove old materials and prep surfaces.", 2, c),
            Item("4a4b4c4d-0101-0101-0101-010101010103", "3a3b3c3d-0101-0101-0101-010101010101", "Build & finish", "Construction, paint, and final touches.", 3, c),

            // ── General Contracting > Deck / patio build ──
            Item("4a4b4c4d-0101-0101-0101-010101010201", "3a3b3c3d-0101-0101-0101-010101010102", "Design & permits", "Draft plan and pull permits.", 1, c),
            Item("4a4b4c4d-0101-0101-0101-010101010202", "3a3b3c3d-0101-0101-0101-010101010102", "Frame & build", "Set footings, frame, and deck boards.", 2, c),
            Item("4a4b4c4d-0101-0101-0101-010101010203", "3a3b3c3d-0101-0101-0101-010101010102", "Finish & inspect", "Stain/seal and final walkthrough.", 3, c),

            // ── Painting > Interior painting ──────────────
            Item("4a4b4c4d-0202-0202-0202-020202020101", "3a3b3c3d-0202-0202-0202-020202020201", "Color consult & prep", "Confirm colors, tape, and cover surfaces.", 1, c),
            Item("4a4b4c4d-0202-0202-0202-020202020102", "3a3b3c3d-0202-0202-0202-020202020201", "Prime & paint", "Apply primer and two coats.", 2, c),
            Item("4a4b4c4d-0202-0202-0202-020202020103", "3a3b3c3d-0202-0202-0202-020202020201", "Touch-up & clean", "Detail edges and remove coverings.", 3, c),

            // ── Painting > Exterior painting ──────────────
            Item("4a4b4c4d-0202-0202-0202-020202020201", "3a3b3c3d-0202-0202-0202-020202020202", "Power wash & scrape", "Clean and prep exterior surfaces.", 1, c),
            Item("4a4b4c4d-0202-0202-0202-020202020202", "3a3b3c3d-0202-0202-0202-020202020202", "Prime & paint", "Coat siding, fascia, and trim.", 2, c),
            Item("4a4b4c4d-0202-0202-0202-020202020203", "3a3b3c3d-0202-0202-0202-020202020202", "Final inspection", "Check coverage and clean site.", 3, c),

            // ── Plumbing > Plumbing repair ────────────────
            Item("4a4b4c4d-0303-0303-0303-030303030101", "3a3b3c3d-0303-0303-0303-030303030301", "Diagnose issue", "Locate leak, blockage, or fault.", 1, c),
            Item("4a4b4c4d-0303-0303-0303-030303030102", "3a3b3c3d-0303-0303-0303-030303030301", "Perform repair", "Fix or replace the faulty component.", 2, c),
            Item("4a4b4c4d-0303-0303-0303-030303030103", "3a3b3c3d-0303-0303-0303-030303030301", "Test & clean up", "Run water, check pressure, tidy area.", 3, c),

            // ── Plumbing > Water heater install ───────────
            Item("4a4b4c4d-0303-0303-0303-030303030201", "3a3b3c3d-0303-0303-0303-030303030302", "Disconnect old unit", "Shut off supply and drain tank.", 1, c),
            Item("4a4b4c4d-0303-0303-0303-030303030202", "3a3b3c3d-0303-0303-0303-030303030302", "Install new unit", "Position, connect plumbing and power.", 2, c),
            Item("4a4b4c4d-0303-0303-0303-030303030203", "3a3b3c3d-0303-0303-0303-030303030302", "Test & verify", "Check for leaks and set temperature.", 3, c),

            // ── Landscaping > Lawn maintenance ────────────
            Item("4a4b4c4d-0404-0404-0404-040404040101", "3a3b3c3d-0404-0404-0404-040404040401", "Mow & edge", "Cut grass and trim borders.", 1, c),
            Item("4a4b4c4d-0404-0404-0404-040404040102", "3a3b3c3d-0404-0404-0404-040404040401", "Blow & clean", "Clear clippings from walks and drives.", 2, c),
            Item("4a4b4c4d-0404-0404-0404-040404040103", "3a3b3c3d-0404-0404-0404-040404040401", "Treat & fertilize", "Apply treatment as needed.", 3, c),

            // ── Landscaping > Garden design & install ─────
            Item("4a4b4c4d-0404-0404-0404-040404040201", "3a3b3c3d-0404-0404-0404-040404040402", "Design layout", "Plan beds, paths, and plant selection.", 1, c),
            Item("4a4b4c4d-0404-0404-0404-040404040202", "3a3b3c3d-0404-0404-0404-040404040402", "Prep & plant", "Amend soil and install plants.", 2, c),
            Item("4a4b4c4d-0404-0404-0404-040404040203", "3a3b3c3d-0404-0404-0404-040404040402", "Mulch & water", "Top-dress and initial watering.", 3, c),

            // ── Electrical > Electrical repair ────────────
            Item("4a4b4c4d-0505-0505-0505-050505050101", "3a3b3c3d-0505-0505-0505-050505050501", "Diagnose fault", "Test circuits and locate issue.", 1, c),
            Item("4a4b4c4d-0505-0505-0505-050505050102", "3a3b3c3d-0505-0505-0505-050505050501", "Repair wiring", "Replace or repair faulty components.", 2, c),
            Item("4a4b4c4d-0505-0505-0505-050505050103", "3a3b3c3d-0505-0505-0505-050505050501", "Test & verify", "Confirm power and safety.", 3, c),

            // ── Electrical > Panel upgrade ────────────────
            Item("4a4b4c4d-0505-0505-0505-050505050201", "3a3b3c3d-0505-0505-0505-050505050502", "Shut down & disconnect", "Kill main and label circuits.", 1, c),
            Item("4a4b4c4d-0505-0505-0505-050505050202", "3a3b3c3d-0505-0505-0505-050505050502", "Swap panel", "Mount new box and reconnect breakers.", 2, c),
            Item("4a4b4c4d-0505-0505-0505-050505050203", "3a3b3c3d-0505-0505-0505-050505050502", "Energize & inspect", "Restore power and run tests.", 3, c),

            // ── Carpentry > Custom shelving ───────────────
            Item("4a4b4c4d-0606-0606-0606-060606060101", "3a3b3c3d-0606-0606-0606-060606060601", "Measure & design", "Take dimensions and plan layout.", 1, c),
            Item("4a4b4c4d-0606-0606-0606-060606060102", "3a3b3c3d-0606-0606-0606-060606060601", "Cut & assemble", "Build shelving units.", 2, c),
            Item("4a4b4c4d-0606-0606-0606-060606060103", "3a3b3c3d-0606-0606-0606-060606060601", "Install & finish", "Mount, sand, and apply finish.", 3, c),

            // ── Carpentry > Trim & molding ────────────────
            Item("4a4b4c4d-0606-0606-0606-060606060201", "3a3b3c3d-0606-0606-0606-060606060602", "Measure & cut", "Measure runs and miter cut pieces.", 1, c),
            Item("4a4b4c4d-0606-0606-0606-060606060202", "3a3b3c3d-0606-0606-0606-060606060602", "Nail & set", "Attach molding and set nails.", 2, c),
            Item("4a4b4c4d-0606-0606-0606-060606060203", "3a3b3c3d-0606-0606-0606-060606060602", "Caulk & paint", "Fill gaps, prime, and paint.", 3, c),

            // ── HVAC > HVAC maintenance ───────────────────
            Item("4a4b4c4d-0707-0707-0707-070707070101", "3a3b3c3d-0707-0707-0707-070707070701", "Inspect & replace filter", "Check airflow and swap filter.", 1, c),
            Item("4a4b4c4d-0707-0707-0707-070707070102", "3a3b3c3d-0707-0707-0707-070707070701", "Clean condenser coils", "Remove buildup from outdoor unit.", 2, c),
            Item("4a4b4c4d-0707-0707-0707-070707070103", "3a3b3c3d-0707-0707-0707-070707070701", "Test thermostat", "Verify settings and calibration.", 3, c),

            // ── HVAC > AC install / replacement ───────────
            Item("4a4b4c4d-0707-0707-0707-070707070201", "3a3b3c3d-0707-0707-0707-070707070702", "Remove old unit", "Disconnect and haul away.", 1, c),
            Item("4a4b4c4d-0707-0707-0707-070707070202", "3a3b3c3d-0707-0707-0707-070707070702", "Install new system", "Set unit, connect lines and electric.", 2, c),
            Item("4a4b4c4d-0707-0707-0707-070707070203", "3a3b3c3d-0707-0707-0707-070707070702", "Charge & test", "Add refrigerant and run cycles.", 3, c),

            // ── Tree Removal > Tree removal ───────────────
            Item("4a4b4c4d-0808-0808-0808-080808080101", "3a3b3c3d-0808-0808-0808-080808080801", "Assess & plan", "Evaluate drop zone and rigging.", 1, c),
            Item("4a4b4c4d-0808-0808-0808-080808080102", "3a3b3c3d-0808-0808-0808-080808080801", "Fell & section", "Cut tree and process limbs.", 2, c),
            Item("4a4b4c4d-0808-0808-0808-080808080103", "3a3b3c3d-0808-0808-0808-080808080801", "Haul & clean", "Remove debris and rake site.", 3, c),

            // ── Tree Removal > Tree trimming ──────────────
            Item("4a4b4c4d-0808-0808-0808-080808080201", "3a3b3c3d-0808-0808-0808-080808080802", "Inspect canopy", "Identify dead or problem branches.", 1, c),
            Item("4a4b4c4d-0808-0808-0808-080808080202", "3a3b3c3d-0808-0808-0808-080808080802", "Prune branches", "Cut for shape, clearance, and health.", 2, c),
            Item("4a4b4c4d-0808-0808-0808-080808080203", "3a3b3c3d-0808-0808-0808-080808080802", "Clean up", "Chip or haul brush.", 3, c),

            // ── Pest Control > General pest treatment ─────
            Item("4a4b4c4d-0909-0909-0909-090909090101", "3a3b3c3d-0909-0909-0909-090909090901", "Inspect property", "Identify pest activity and entry points.", 1, c),
            Item("4a4b4c4d-0909-0909-0909-090909090102", "3a3b3c3d-0909-0909-0909-090909090901", "Apply treatment", "Spray interior and exterior perimeter.", 2, c),
            Item("4a4b4c4d-0909-0909-0909-090909090103", "3a3b3c3d-0909-0909-0909-090909090901", "Document & recommend", "Log findings and next visit.", 3, c),

            // ── Pest Control > Termite inspection ─────────
            Item("4a4b4c4d-0909-0909-0909-090909090201", "3a3b3c3d-0909-0909-0909-090909090902", "Full property scan", "Check foundation, crawl, and attic.", 1, c),
            Item("4a4b4c4d-0909-0909-0909-090909090202", "3a3b3c3d-0909-0909-0909-090909090902", "Probe suspect areas", "Tap and test for damage.", 2, c),
            Item("4a4b4c4d-0909-0909-0909-090909090203", "3a3b3c3d-0909-0909-0909-090909090902", "Deliver report", "Findings and treatment estimate.", 3, c),

            // ── Cleaning > Full home clean ────────────────
            Item("4a4b4c4d-1010-1010-1010-101010100101", "3a3b3c3d-1010-1010-1010-101010101001", "Walkthrough", "Assess rooms and special requests.", 1, c),
            Item("4a4b4c4d-1010-1010-1010-101010100102", "3a3b3c3d-1010-1010-1010-101010101001", "Deep clean kitchen & baths", "Scrub fixtures, counters, appliances.", 2, c),
            Item("4a4b4c4d-1010-1010-1010-101010100103", "3a3b3c3d-1010-1010-1010-101010101001", "Vacuum & mop", "All hard and carpeted surfaces.", 3, c),

            // ── Cleaning > Move-out deep clean ────────────
            Item("4a4b4c4d-1010-1010-1010-101010100201", "3a3b3c3d-1010-1010-1010-101010101002", "Empty & prep", "Clear remaining items, dust surfaces.", 1, c),
            Item("4a4b4c4d-1010-1010-1010-101010100202", "3a3b3c3d-1010-1010-1010-101010101002", "Scrub all rooms", "Walls, baseboards, inside cabinets.", 2, c),
            Item("4a4b4c4d-1010-1010-1010-101010100203", "3a3b3c3d-1010-1010-1010-101010101002", "Final walkthrough", "Verify quality before handoff.", 3, c),

            // ── Junk Removal > Full property cleanout ─────
            Item("4a4b4c4d-1111-1111-1111-111111110101", "3a3b3c3d-1111-1111-1111-111111111101", "Walk & inventory", "Tag items and plan truck loads.", 1, c),
            Item("4a4b4c4d-1111-1111-1111-111111110102", "3a3b3c3d-1111-1111-1111-111111111101", "Load & haul", "Remove items to truck.", 2, c),
            Item("4a4b4c4d-1111-1111-1111-111111110103", "3a3b3c3d-1111-1111-1111-111111111101", "Sweep & dispose", "Clean site, dump or donate.", 3, c),

            // ── Junk Removal > Garage cleanout ────────────
            Item("4a4b4c4d-1111-1111-1111-111111110201", "3a3b3c3d-1111-1111-1111-111111111102", "Sort items", "Separate keep, donate, and trash.", 1, c),
            Item("4a4b4c4d-1111-1111-1111-111111110202", "3a3b3c3d-1111-1111-1111-111111111102", "Load out", "Move discards to truck.", 2, c),
            Item("4a4b4c4d-1111-1111-1111-111111110203", "3a3b3c3d-1111-1111-1111-111111111102", "Sweep garage", "Broom clean and organize keepers.", 3, c),

            // ── Car Detailing > Full detail ───────────────
            Item("4a4b4c4d-1212-1212-1212-121212120101", "3a3b3c3d-1212-1212-1212-121212121201", "Exterior wash & clay", "Hand wash, clay bar, and dry.", 1, c),
            Item("4a4b4c4d-1212-1212-1212-121212120102", "3a3b3c3d-1212-1212-1212-121212121201", "Interior deep clean", "Vacuum, shampoo, and condition.", 2, c),
            Item("4a4b4c4d-1212-1212-1212-121212120103", "3a3b3c3d-1212-1212-1212-121212121201", "Polish & protect", "Compound, wax, and dress tires.", 3, c),

            // ── Car Detailing > Interior only ─────────────
            Item("4a4b4c4d-1212-1212-1212-121212120201", "3a3b3c3d-1212-1212-1212-121212121202", "Vacuum & dust", "All seats, dash, and crevices.", 1, c),
            Item("4a4b4c4d-1212-1212-1212-121212120202", "3a3b3c3d-1212-1212-1212-121212121202", "Shampoo upholstery", "Extract and clean fabric or leather.", 2, c),
            Item("4a4b4c4d-1212-1212-1212-121212120203", "3a3b3c3d-1212-1212-1212-121212121202", "Glass & final wipe", "Clean interior glass and surfaces.", 3, c),

            // ── IT & Network > Network setup ──────────────
            Item("4a4b4c4d-1313-1313-1313-131313130101", "3a3b3c3d-1313-1313-1313-131313131301", "Site survey", "Map runs and plan equipment placement.", 1, c),
            Item("4a4b4c4d-1313-1313-1313-131313130102", "3a3b3c3d-1313-1313-1313-131313131301", "Run cable & mount", "Pull cable and install hardware.", 2, c),
            Item("4a4b4c4d-1313-1313-1313-131313130103", "3a3b3c3d-1313-1313-1313-131313131301", "Configure & test", "Set up network and verify connectivity.", 3, c),

            // ── IT & Network > Workstation deployment ─────
            Item("4a4b4c4d-1313-1313-1313-131313130201", "3a3b3c3d-1313-1313-1313-131313131302", "Unbox & image", "Prep hardware and install OS/software.", 1, c),
            Item("4a4b4c4d-1313-1313-1313-131313130202", "3a3b3c3d-1313-1313-1313-131313131302", "Deploy & connect", "Place at desk and join network.", 2, c),
            Item("4a4b4c4d-1313-1313-1313-131313130203", "3a3b3c3d-1313-1313-1313-131313131302", "User walkthrough", "Verify apps and orient user.", 3, c),

            // ── Handyman > General repair ─────────────────
            Item("4a4b4c4d-1414-1414-1414-141414140101", "3a3b3c3d-1414-1414-1414-141414141401", "Assess issue", "Inspect problem area and plan fix.", 1, c),
            Item("4a4b4c4d-1414-1414-1414-141414140102", "3a3b3c3d-1414-1414-1414-141414141401", "Repair", "Fix or replace components.", 2, c),
            Item("4a4b4c4d-1414-1414-1414-141414140103", "3a3b3c3d-1414-1414-1414-141414141401", "Verify & clean up", "Test fix and tidy work area.", 3, c),

            // ── Handyman > Fixture install ────────────────
            Item("4a4b4c4d-1414-1414-1414-141414140201", "3a3b3c3d-1414-1414-1414-141414141402", "Prep location", "Mark placement and run wiring if needed.", 1, c),
            Item("4a4b4c4d-1414-1414-1414-141414140202", "3a3b3c3d-1414-1414-1414-141414141402", "Mount fixture", "Secure and connect.", 2, c),
            Item("4a4b4c4d-1414-1414-1414-141414140203", "3a3b3c3d-1414-1414-1414-141414141402", "Test & finish", "Confirm operation and patch holes.", 3, c),

            // ── Flooring > Hardwood install ───────────────
            Item("4a4b4c4d-1515-1515-1515-151515150101", "3a3b3c3d-1515-1515-1515-151515151501", "Prep subfloor", "Level, clean, and lay moisture barrier.", 1, c),
            Item("4a4b4c4d-1515-1515-1515-151515150102", "3a3b3c3d-1515-1515-1515-151515151501", "Install planks", "Lay and nail or click into place.", 2, c),
            Item("4a4b4c4d-1515-1515-1515-151515150103", "3a3b3c3d-1515-1515-1515-151515151501", "Trim & finish", "Install transitions and clean.", 3, c),

            // ── Flooring > Tile install ───────────────────
            Item("4a4b4c4d-1515-1515-1515-151515150201", "3a3b3c3d-1515-1515-1515-151515151502", "Layout & prep", "Dry-fit pattern and mix thinset.", 1, c),
            Item("4a4b4c4d-1515-1515-1515-151515150202", "3a3b3c3d-1515-1515-1515-151515151502", "Set tile", "Lay tiles with spacers.", 2, c),
            Item("4a4b4c4d-1515-1515-1515-151515150203", "3a3b3c3d-1515-1515-1515-151515151502", "Grout & seal", "Fill joints, clean haze, and seal.", 3, c)
        );
    }

    private static JobTemplateItem Item(string id, string templateId, string name, string description, int sort, DateTime createdAt)
    {
        return new JobTemplateItem
        {
            Id = Guid.Parse(id),
            TemplateId = Guid.Parse(templateId),
            Name = name,
            Description = description,
            SortOrder = sort,
            CreatedAt = createdAt,
            IsActive = true
        };
    }
}
