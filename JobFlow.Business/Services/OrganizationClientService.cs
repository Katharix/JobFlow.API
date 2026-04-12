using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Utilities;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class OrganizationClientService : IOrganizationClientService
{
    private readonly ILogger<OrganizationClientService> logger;
    private readonly IRepository<OrganizationClient> organizationClient;
    private readonly IOnboardingService onboardingService;
    private readonly IOrganizationClientPortalService _clientPortal;
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper _mapper;

    public OrganizationClientService(
        ILogger<OrganizationClientService> logger,
        IUnitOfWork unitOfWork,
        IOnboardingService onboardingService,
        IOrganizationClientPortalService clientPortal,
        IMapper mapper)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        this.onboardingService = onboardingService;
        _clientPortal = clientPortal;
        organizationClient = this.unitOfWork.RepositoryOf<OrganizationClient>();
        _mapper = mapper;
    }

    public async Task<Result> DeleteClient(Guid clientId)
    {
        var clientToDelete = await organizationClient.Query()
            .FirstOrDefaultAsync(client => client.Id == clientId);

        if (clientToDelete == null)
            return Result.Failure(OrganizationClientErrors.NoClientFound);

        var clientName = clientToDelete.ClientFullName();
        organizationClient.Remove(clientToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"{clientName} was successfully removed.");
    }

    public async Task<Result> DeleteClient(Guid clientId, Guid organizationId)
    {
        var clientToDelete = await organizationClient.Query()
            .FirstOrDefaultAsync(client => client.Id == clientId && client.OrganizationId == organizationId);

        if (clientToDelete == null)
            return Result.Failure(OrganizationClientErrors.NoClientFound);

        var clientName = clientToDelete.ClientFullName();
        organizationClient.Remove(clientToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"{clientName} was successfully removed.");
    }

    public async Task<Result<IEnumerable<OrganizationClient>>> GetAllClients()
    {
        var clients = await organizationClient.Query().ToListAsync();
        return Result.Success<IEnumerable<OrganizationClient>>(clients);
    }

    public async Task<Result<IEnumerable<OrganizationClient>>> GetAllClientsByOrganizationId(Guid organizationId)
    {
        var clients = await organizationClient.Query().Where(client => client.OrganizationId == organizationId)
            .ToListAsync();
        return Result.Success<IEnumerable<OrganizationClient>>(clients);
    }

    public async Task<Result<CursorPagedResponseDto<OrganizationClient>>> GetClientsByOrganizationPagedAsync(
        Guid organizationId,
        int pageSize,
        string? cursor,
        bool missingEmailOnly,
        string? search,
        string? sortBy,
        string? sortDirection)
    {
        var size = Math.Clamp(pageSize, 1, 100);
        var query = organizationClient.Query()
            .AsNoTracking()
            .Where(client => client.OrganizationId == organizationId);

        if (missingEmailOnly)
        {
            query = query.Where(c => c.EmailAddress == null || c.EmailAddress.Trim() == string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.FirstName, $"%{term}%")
                || EF.Functions.Like(c.LastName, $"%{term}%")
                || (c.EmailAddress != null && EF.Functions.Like(c.EmailAddress, $"%{term}%"))
                || (c.PhoneNumber != null && EF.Functions.Like(c.PhoneNumber, $"%{term}%")));
        }

        var desc = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "firstname" => desc ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Id) : query.OrderBy(c => c.FirstName).ThenBy(c => c.Id),
            "lastname" => desc ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.Id) : query.OrderBy(c => c.LastName).ThenBy(c => c.Id),
            "email" => desc ? query.OrderByDescending(c => c.EmailAddress).ThenByDescending(c => c.Id) : query.OrderBy(c => c.EmailAddress).ThenBy(c => c.Id),
            _ => desc ? query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id) : query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
        };

        var totalCount = await query.CountAsync();

        if (CursorToken.TryRead(cursor, out var cursorCreatedAt, out var cursorId))
        {
            query = query.Where(c => c.CreatedAt < cursorCreatedAt || (c.CreatedAt == cursorCreatedAt && c.Id.CompareTo(cursorId) < 0));
        }

        var batch = await query
            .Take(size + 1)
            .ToListAsync();

        var hasMore = batch.Count > size;
        var items = hasMore ? batch.Take(size).ToList() : batch;
        var nextCursor = hasMore && items.Count > 0
            ? CursorToken.Build(items[^1].CreatedAt, items[^1].Id)
            : null;

        return Result.Success(new CursorPagedResponseDto<OrganizationClient>
        {
            Items = items,
            NextCursor = nextCursor,
            TotalCount = totalCount
        });
    }

    public async Task<Result<OrganizationClient>> GetClientById(Guid clientId)
    {
        var client = await organizationClient.Query().FirstOrDefaultAsync(cli => cli.Id == clientId);
        if (client == null) return Result.Failure<OrganizationClient>(OrganizationClientErrors.NoClientFound);

        return Result.Success<OrganizationClient>(client);
    }

    public async Task<Result<OrganizationClient>> GetOrganizationClientByEmailAsync(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return Result.Failure<OrganizationClient>(OrganizationClientErrors.NoClientFound);

        var normalized = emailAddress.Trim().ToLowerInvariant();

        var match = await organizationClient.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmailAddress != null && c.EmailAddress.ToLower() == normalized);

        return match is null
            ? Result.Failure<OrganizationClient>(OrganizationClientErrors.NoClientFound)
            : Result.Success(match);
    }

    public async Task<Result<IReadOnlyList<OrganizationClient>>> GetOrganizationClientsByEmailAsync(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return Result.Failure<IReadOnlyList<OrganizationClient>>(OrganizationClientErrors.NoClientFound);

        var normalized = emailAddress.Trim().ToLowerInvariant();

        var matches = await organizationClient.Query()
            .AsNoTracking()
            .Include(c => c.Organization)
            .Where(c => c.EmailAddress != null && c.EmailAddress.ToLower() == normalized)
            .ToListAsync();

        return matches.Count == 0
            ? Result.Failure<IReadOnlyList<OrganizationClient>>(OrganizationClientErrors.NoClientFound)
            : Result.Success<IReadOnlyList<OrganizationClient>>(matches);
    }

    public async Task<Result<OrganizationClient>> UpsertClient(OrganizationClient model)
    {
        if (!string.IsNullOrWhiteSpace(model.EmailAddress))
        {
            var duplicate = await organizationClient.Query()
                .AnyAsync(c => c.OrganizationId == model.OrganizationId
                    && c.EmailAddress != null
                    && c.EmailAddress.ToLower() == model.EmailAddress.ToLower()
                    && c.Id != model.Id);

            if (duplicate)
                return Result.Failure<OrganizationClient>(OrganizationClientErrors.DuplicateEmail(model.EmailAddress));
        }

        var exists = await organizationClient.Query()
            .AnyAsync(c => c.Id == model.Id);

        if (exists)
        {
            organizationClient.Update(model);
        }
        else
        {
            await organizationClient.AddAsync(model);
        }

        await unitOfWork.SaveChangesAsync();

        if (!exists)
        {
            await onboardingService.MarkStepCompleteAsync(
                model.OrganizationId,
                OnboardingStepKeys.CreateCustomer
            );

            if (!string.IsNullOrWhiteSpace(model.EmailAddress))
            {
                try
                {
                    await _clientPortal.SendMagicLinkAsync(model.OrganizationId, model.Id, model.EmailAddress);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send client portal magic link for OrganizationClient {ClientId}", model.Id);
                }
            }
        }
        return Result.Success(model);
    }

    public async Task<Result<IEnumerable<OrganizationClient>>> UpsertMultipleClients(
        IEnumerable<OrganizationClient> modelList)
    {
        var models = modelList.ToList();
        var modelsToInsert = models.Where(client => client.Id == Guid.Empty).ToList();
        var modelsToUpdate = models.Where(client => client.Id != Guid.Empty).ToList();

        if (models.Count == 0)
            return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.FailedToCreateClient);

        // Check for duplicate emails within the batch itself
        var batchEmails = models
            .Where(c => !string.IsNullOrWhiteSpace(c.EmailAddress))
            .GroupBy(c => c.EmailAddress!.ToLower())
            .FirstOrDefault(g => g.Count() > 1);

        if (batchEmails != null)
            return Result.Failure<IEnumerable<OrganizationClient>>(
                OrganizationClientErrors.DuplicateEmail(batchEmails.Key));

        // Check for duplicate emails against existing clients in the organization
        var emails = models
            .Where(c => !string.IsNullOrWhiteSpace(c.EmailAddress))
            .Select(c => c.EmailAddress!.ToLower())
            .ToList();

        if (emails.Count > 0)
        {
            var ids = models.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToList();
            var existingDuplicate = await organizationClient.Query()
                .Where(c => c.OrganizationId == models[0].OrganizationId
                    && c.EmailAddress != null
                    && emails.Contains(c.EmailAddress.ToLower())
                    && !ids.Contains(c.Id))
                .Select(c => c.EmailAddress)
                .FirstOrDefaultAsync();

            if (existingDuplicate != null)
                return Result.Failure<IEnumerable<OrganizationClient>>(
                    OrganizationClientErrors.DuplicateEmail(existingDuplicate));
        }

        if (modelsToInsert.Count > 0) organizationClient.AddRange(modelsToInsert);
        if (modelsToUpdate.Count > 0) organizationClient.UpdateRange(modelsToUpdate);

        await unitOfWork.SaveChangesAsync();
        return Result.Success<IEnumerable<OrganizationClient>>(models.AsEnumerable());
    }

    public async Task<Result> RestoreClient(Guid clientId, Guid organizationId)
    {
        var clientToRestore = await organizationClient.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(client => client.Id == clientId && client.OrganizationId == organizationId);

        if (clientToRestore == null)
            return Result.Failure(OrganizationClientErrors.NoClientFound);

        clientToRestore.IsActive = true;
        clientToRestore.DeactivatedAtUtc = null;

        organizationClient.Update(clientToRestore);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"{clientToRestore.ClientFullName()} was successfully restored.");
    }

}