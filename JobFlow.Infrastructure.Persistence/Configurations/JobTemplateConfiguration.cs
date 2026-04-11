using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class JobTemplateConfiguration : IEntityTypeConfiguration<JobTemplate>
{
    public void Configure(EntityTypeBuilder<JobTemplate> builder)
    {
        builder.ToTable("JobTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(t => t.Description)
            .HasMaxLength(240);
        builder.HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.OrganizationType)
            .WithMany()
            .HasForeignKey(t => t.OrganizationTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(t => t.IsActive);

        var createdAt = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);

        // ── Organization Type GUIDs (from OrganizationTypeConfiguration) ──
        var generalContracting = Guid.Parse("bf489aa6-db19-42df-82bc-c116bd967e7e");
        var painting = Guid.Parse("393a5b3e-323e-4b76-aa86-0d4683ddcd49");
        var plumbing = Guid.Parse("e01750b0-0d01-4e25-abf7-6efa23509035");
        var landscaping = Guid.Parse("fbc6accf-0fb1-4908-b449-14f13b826f24");
        var electrical = Guid.Parse("9362c957-0f41-4c20-9085-c01e449fdda2");
        var carpentry = Guid.Parse("8f0d3e93-425b-4a53-b4d2-4c5eb97e490f");
        var hvac = Guid.Parse("37fc17e8-0a25-4119-9a71-7d160bb9c7b4");
        var treeRemoval = Guid.Parse("f64f078f-ecfb-4f3e-8640-236219fcf01e");
        var pestControl = Guid.Parse("bf3b9512-8a9c-4a73-9f88-cb914c1573cd");
        var cleaning = Guid.Parse("1921d982-22f8-4ed5-b4e3-fca82c5767eb");
        var junkRemoval = Guid.Parse("408d2185-53b9-493d-8713-938114de90f5");
        var carDetailing = Guid.Parse("33341b2d-957f-4efb-94f7-3a015ae1a718");
        var itNetwork = Guid.Parse("30530a32-a151-436d-a050-613eac4c22d5");
        var handyman = Guid.Parse("0F32E14A-5F70-45AF-A647-04E59AD52E58");
        var flooring = Guid.Parse("09786EAB-D69F-45BF-BCEC-5F368BD60BE7");

        builder.HasData(
            // ── General Contracting ───────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0101-0101-0101-010101010101"),
                Name = "Home renovation",
                Description = "Full or partial home remodel project.",
                OrganizationTypeId = generalContracting,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0101-0101-0101-010101010102"),
                Name = "Deck / patio build",
                Description = "New outdoor living space construction.",
                OrganizationTypeId = generalContracting,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Painting ─────────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0202-0202-0202-020202020201"),
                Name = "Interior painting",
                Description = "Paint walls, ceilings, and trim for interior rooms.",
                OrganizationTypeId = painting,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0202-0202-0202-020202020202"),
                Name = "Exterior painting",
                Description = "Prep and paint home exterior surfaces.",
                OrganizationTypeId = painting,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Plumbing ─────────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0303-0303-0303-030303030301"),
                Name = "Plumbing repair",
                Description = "Diagnose and fix leaks, clogs, or faults.",
                OrganizationTypeId = plumbing,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0303-0303-0303-030303030302"),
                Name = "Water heater install",
                Description = "Remove old unit and install replacement.",
                OrganizationTypeId = plumbing,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Landscaping and Gardening ─────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0404-0404-0404-040404040401"),
                Name = "Lawn maintenance",
                Description = "Mow, edge, blow, and treat lawn.",
                OrganizationTypeId = landscaping,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0404-0404-0404-040404040402"),
                Name = "Garden design & install",
                Description = "Design beds, select plants, and install.",
                OrganizationTypeId = landscaping,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Electrical Services ───────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0505-0505-0505-050505050501"),
                Name = "Electrical repair",
                Description = "Troubleshoot and fix wiring or fixture issues.",
                OrganizationTypeId = electrical,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0505-0505-0505-050505050502"),
                Name = "Panel upgrade",
                Description = "Upgrade electrical panel for capacity or safety.",
                OrganizationTypeId = electrical,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Carpentry and Woodworking ─────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0606-0606-0606-060606060601"),
                Name = "Custom shelving",
                Description = "Measure, build, and install custom shelves.",
                OrganizationTypeId = carpentry,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0606-0606-0606-060606060602"),
                Name = "Trim & molding install",
                Description = "Install baseboards, crown molding, and casings.",
                OrganizationTypeId = carpentry,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── HVAC Services ─────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0707-0707-0707-070707070701"),
                Name = "HVAC maintenance",
                Description = "Seasonal heating/cooling system tune-up.",
                OrganizationTypeId = hvac,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0707-0707-0707-070707070702"),
                Name = "AC install / replacement",
                Description = "Remove old unit and install new AC system.",
                OrganizationTypeId = hvac,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Tree Removal ──────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0808-0808-0808-080808080801"),
                Name = "Tree removal",
                Description = "Fell, section, and haul away tree.",
                OrganizationTypeId = treeRemoval,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0808-0808-0808-080808080802"),
                Name = "Tree trimming",
                Description = "Prune branches for health and clearance.",
                OrganizationTypeId = treeRemoval,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Pest Control ──────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0909-0909-0909-090909090901"),
                Name = "General pest treatment",
                Description = "Interior and exterior spray for common pests.",
                OrganizationTypeId = pestControl,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-0909-0909-0909-090909090902"),
                Name = "Termite inspection",
                Description = "Full property inspection and treatment plan.",
                OrganizationTypeId = pestControl,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Cleaning Services ─────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1010-1010-1010-101010101001"),
                Name = "Full home clean",
                Description = "Standard whole-house cleaning visit.",
                OrganizationTypeId = cleaning,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1010-1010-1010-101010101002"),
                Name = "Move-out deep clean",
                Description = "Thorough clean for tenant turnover.",
                OrganizationTypeId = cleaning,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Junk Removal ──────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1111-1111-1111-111111111101"),
                Name = "Full property cleanout",
                Description = "Remove all unwanted items from property.",
                OrganizationTypeId = junkRemoval,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1111-1111-1111-111111111102"),
                Name = "Garage cleanout",
                Description = "Clear and haul garage contents.",
                OrganizationTypeId = junkRemoval,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Car Detailing ─────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1212-1212-1212-121212121201"),
                Name = "Full detail",
                Description = "Complete interior and exterior detail.",
                OrganizationTypeId = carDetailing,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1212-1212-1212-121212121202"),
                Name = "Interior only",
                Description = "Deep clean seats, dash, carpet, and glass.",
                OrganizationTypeId = carDetailing,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── IT & Network Installation ─────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1313-1313-1313-131313131301"),
                Name = "Network setup",
                Description = "Install and configure router, switches, and cabling.",
                OrganizationTypeId = itNetwork,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1313-1313-1313-131313131302"),
                Name = "Workstation deployment",
                Description = "Set up desktops, laptops, and peripherals.",
                OrganizationTypeId = itNetwork,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Handyman ──────────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1414-1414-1414-141414141401"),
                Name = "General repair",
                Description = "Fix miscellaneous issues around the home.",
                OrganizationTypeId = handyman,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1414-1414-1414-141414141402"),
                Name = "Fixture install",
                Description = "Mount and connect lights, fans, or hardware.",
                OrganizationTypeId = handyman,
                DefaultInvoicingWorkflow = InvoicingWorkflow.InPerson,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },

            // ── Flooring ──────────────────────────────────
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1515-1515-1515-151515151501"),
                Name = "Hardwood install",
                Description = "Measure, prep subfloor, and install hardwood.",
                OrganizationTypeId = flooring,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new JobTemplate
            {
                Id = Guid.Parse("3a3b3c3d-1515-1515-1515-151515151502"),
                Name = "Tile install",
                Description = "Lay tile with proper spacing and grout.",
                OrganizationTypeId = flooring,
                DefaultInvoicingWorkflow = InvoicingWorkflow.SendInvoice,
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            }
        );
    }
}
