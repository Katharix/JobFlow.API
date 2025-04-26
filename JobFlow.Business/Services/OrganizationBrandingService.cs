using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobFlow.Business.DI;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class OrganizationBrandingService : IOrganizationBrandingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<OrganizationBranding> _brandingRepo;
        private readonly ILogger<OrganizationBrandingService> _logger;

        public OrganizationBrandingService(
            IUnitOfWork unitOfWork,
            ILogger<OrganizationBrandingService> logger)
        {
            _unitOfWork = unitOfWork;
            _brandingRepo = _unitOfWork.RepositoryOf<OrganizationBranding>();
            _logger = logger;
        }

        public async Task<Result<OrganizationBranding>> GetByOrganizationIdAsync(Guid organizationId)
        {
            try
            {
                var branding = await _brandingRepo
                    .Query()
                    .FirstOrDefaultAsync(b => b.OrganizationId == organizationId);

                return branding is null
                    ? Result.Failure<OrganizationBranding>(Error.NotFound("","Branding not found."))
                    : Result.Success(branding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branding for org {OrgId}", organizationId);
                return Result.Failure<OrganizationBranding>(Error.Conflict("", "Failed to fetch branding settings."));
            }
        }


        public async Task<Result<OrganizationBranding>> CreateOrUpdateAsync(OrganizationBranding model)
        {
            try
            {
                var existing = await _brandingRepo
                    .Query()
                    .FirstOrDefaultAsync(b => b.OrganizationId == model.OrganizationId);

                if (existing is null)
                {
                    model.CreatedAt = DateTime.UtcNow;
                    await _brandingRepo.AddAsync(model);
                }
                else
                {
                    existing.LogoUrl = model.LogoUrl;
                    existing.PrimaryColor = model.PrimaryColor;
                    existing.SecondaryColor = model.SecondaryColor;
                    existing.BusinessName = model.BusinessName;
                    existing.Tagline = model.Tagline;
                    existing.FooterNote = model.FooterNote;
                    existing.UpdatedAt = DateTime.UtcNow;

                    _brandingRepo.Update(existing);
                }

                await _unitOfWork.SaveChangesAsync();
                return Result.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating branding for org {OrgId}", model.OrganizationId);
                return Result.Failure<OrganizationBranding>(Error.Failure("","Failed to save branding settings."));
            }
        }
    }
}