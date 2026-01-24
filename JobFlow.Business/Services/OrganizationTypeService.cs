using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class OrganizationTypeService : IOrganizationTypeService
{
    private readonly ILogger<OrganizationTypeService> logger;
    private readonly IRepository<OrganizationType> organizationTypes;
    private readonly IUnitOfWork unitOfWork;

    public OrganizationTypeService(ILogger<OrganizationTypeService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        organizationTypes = this.unitOfWork.RepositoryOf<OrganizationType>();
    }

    public async Task<Result> DeleteMultipleOrganizationTypes(IEnumerable<Guid> idList)
    {
        var organizationTypesToDelete = organizationTypes.Query().Where(org => idList.Contains(org.Id));
        if (!organizationTypesToDelete.Any()) return Result.Failure(OrganizationTypeErrors.OrganizationTypeNotFound);
        var organizationTypeNames = organizationTypesToDelete.Select(org => org.TypeName);
        var commaDelimitedOrganizationTypeNames = string.Join(", ", organizationTypeNames);

        organizationTypes.RemoveRange(organizationTypesToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"The following types were deleted successfully: {commaDelimitedOrganizationTypeNames}.");
    }

    public async Task<Result> DeleteOrganizationType(Guid organizationTypeId)
    {
        var organizationTypeToDelete = organizationTypes.Query().FirstOrDefault(org => org.Id == organizationTypeId);
        if (organizationTypeToDelete == null) return Result.Failure(OrganizationTypeErrors.OrganizationTypeNotFound);

        organizationTypes.Remove(organizationTypeToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"The {organizationTypeToDelete.TypeName} type has been deleted successfully.");
    }

    public async Task<Result<OrganizationType>> GetTypeById(Guid organizationTypeId)
    {
        var organizationType = await organizationTypes.Query().FirstOrDefaultAsync(org => org.Id == organizationTypeId);
        if (organizationType == null)
            return Result.Failure<OrganizationType>(OrganizationTypeErrors.OrganizationTypeNotFound);

        return Result.Success<OrganizationType>(organizationType);
    }

    public async Task<Result<IEnumerable<OrganizationType>>> GetTypes()
    {
        var organizationTypesList = organizationTypes.Query();
        if (!organizationTypesList.Any())
            return Result.Failure<IEnumerable<OrganizationType>>(OrganizationTypeErrors.OrganizationTypeNotFound);

        return Result.Success<IEnumerable<OrganizationType>>(organizationTypesList);
    }

    public async Task<Result<IEnumerable<OrganizationType>>> UpsertOrganizationList(
        IEnumerable<OrganizationType> modelList)
    {
        if (modelList == null || !modelList.Any())
            return Result.Failure<IEnumerable<OrganizationType>>(OrganizationTypeErrors.NoOrganizationTypesToUpsert);

        var modelsToInsert = new List<OrganizationType>();
        var modelsToUpdate = new List<OrganizationType>();

        foreach (var org in modelList)
            if (org.Id == Guid.Empty)
                modelsToInsert.Add(org);
            else
                modelsToUpdate.Add(org);

        if (modelsToInsert.Count > 0) organizationTypes.AddRange(modelsToInsert);

        if (modelsToUpdate.Count > 0) organizationTypes.UpdateRange(modelsToUpdate);

        await unitOfWork.SaveChangesAsync();
        return Result.Success(modelList);
    }


    public async Task<Result<OrganizationType>> UpsertOrganizationType(OrganizationType model)
    {
        if (model.Id == Guid.Empty)
            organizationTypes.Add(model);
        else
            organizationTypes.Update(model);

        await unitOfWork.SaveChangesAsync();
        return Result.Success<OrganizationType>(model);
    }
}