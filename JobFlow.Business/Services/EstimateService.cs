using System.Security.Cryptography;
using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class EstimateService : IEstimateService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly INotificationService notificationService;
    private readonly IPdfGenerator pdfGenerator;
    private readonly IOrganizationClientPortalService clientPortalService;
    private readonly IFollowUpAutomationService? _followUpAutomation;
    private readonly IJobService _jobService;

    private readonly IRepository<Estimate> estimates;
    private readonly IRepository<OrganizationClient> clients;

    public EstimateService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IPdfGenerator pdfGenerator,
        IOrganizationClientPortalService clientPortalService,
        IJobService jobService,
        IFollowUpAutomationService? followUpAutomation = null)
    {
        this.unitOfWork = unitOfWork;
        this.notificationService = notificationService;
        this.pdfGenerator = pdfGenerator;
        this.clientPortalService = clientPortalService;
        _jobService = jobService;
        _followUpAutomation = followUpAutomation;

        estimates = unitOfWork.RepositoryOf<Estimate>();
        clients = unitOfWork.RepositoryOf<OrganizationClient>();
    }

    public async Task<Result<EstimateDto>> GetByIdAsync(Guid id)
    {
        var estimate = await estimates.Query()
            .Include(x => x.OrganizationClient)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (estimate == null)
            return Result.Failure<EstimateDto>(EstimateErrors.NotFound);

        return Result<EstimateDto>.Success(ToDto(estimate));
    }

    public async Task<Result<IEnumerable<EstimateDto>>> GetByOrganizationAsync(Guid organizationId)
    {
        var list = await estimates.Query()
            .Where(x => x.OrganizationId == organizationId)
            .Include(x => x.OrganizationClient)
            .Include(x => x.LineItems)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Result<IEnumerable<EstimateDto>>.Success(list.Select(ToDto));
    }

    public async Task<Result<EstimateDto>> CreateAsync(CreateEstimateRequest request)
    {
        if (request.LineItems == null || request.LineItems.Count == 0)
            return Result.Failure<EstimateDto>(EstimateErrors.InvalidLineItems);

        var client = await clients.Query().FirstOrDefaultAsync(x => x.Id == request.OrganizationClientId);
        if (client == null)
            return Result.Failure<EstimateDto>(EstimateErrors.ClientNotFound);

        var estimate = new Estimate
        {
            OrganizationId = request.OrganizationId,
            OrganizationClientId = request.OrganizationClientId,
            EstimateNumber = await GenerateEstimateNumberAsync(request.OrganizationId),
            Title = request.Title,
            Description = request.Description,
            Notes = request.Notes,
            Status = EstimateStatus.Draft,
            PublicToken = GeneratePublicToken(),
            PublicTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow,
            OrganizationClient = client
        };

        foreach (var li in request.LineItems)
        {
            estimate.LineItems.Add(new EstimateLineItem
            {
                Name = li.Name,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Total = Math.Round(li.Quantity * li.UnitPrice, 2)
            });
        }

        RecalculateTotals(estimate);

        await estimates.AddAsync(estimate);
        await unitOfWork.SaveChangesAsync();

        return Result<EstimateDto>.Success(ToDto(estimate));
    }

    public async Task<Result<EstimateDto>> UpdateAsync(Guid id, UpdateEstimateRequest request)
    {
        if (request.LineItems == null || request.LineItems.Count == 0)
            return Result.Failure<EstimateDto>(EstimateErrors.InvalidLineItems);

        var estimate = await estimates.Query()
            .Include(x => x.OrganizationClient)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (estimate == null)
            return Result.Failure<EstimateDto>(EstimateErrors.NotFound);

        estimate.Title = request.Title;
        estimate.Description = request.Description;
        estimate.Notes = request.Notes;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;

        estimate.LineItems.Clear();
        foreach (var li in request.LineItems)
        {
            estimate.LineItems.Add(new EstimateLineItem
            {
                Name = li.Name,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Total = Math.Round(li.Quantity * li.UnitPrice, 2)
            });
        }

        RecalculateTotals(estimate);

        estimates.Update(estimate);
        await unitOfWork.SaveChangesAsync();

        return Result<EstimateDto>.Success(ToDto(estimate));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var estimate = await estimates.Query().FirstOrDefaultAsync(x => x.Id == id);
        if (estimate == null)
            return Result.Failure(EstimateErrors.NotFound);

        estimates.Remove(estimate);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<EstimateDto>> SendAsync(Guid id, SendEstimateRequest request)
    {
        var estimate = await estimates.Query().FirstOrDefaultAsync(x => x.Id == id);
        if (estimate == null)
            return Result.Failure<EstimateDto>(EstimateErrors.NotFound);

        var client = await clients.Query()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == estimate.OrganizationClientId);

        if (client == null)
            return Result.Failure<EstimateDto>(EstimateErrors.ClientNotFound);

        if (string.IsNullOrWhiteSpace(estimate.PublicToken))
            estimate.PublicToken = GeneratePublicToken();

        estimate.PublicTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        estimate.Status = EstimateStatus.Sent;
        estimate.SentAt = DateTimeOffset.UtcNow;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;

        estimates.Update(estimate);
        await unitOfWork.SaveChangesAsync();

        if (_followUpAutomation != null)
            await _followUpAutomation.StartEstimateSequenceAsync(
                estimate.OrganizationId,
                estimate.Id,
                estimate.OrganizationClientId);

        await clientPortalService.SendMagicLinkAsync(estimate.OrganizationId, client.Id, client.EmailAddress ?? string.Empty);

        var full = await estimates.Query()
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == estimate.Id);

        return Result<EstimateDto>.Success(ToDto(full!));
    }

    public async Task<Result<EstimateDto>> GetByPublicTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<EstimateDto>(EstimateErrors.InvalidPublicLink);

        var estimate = await estimates.Query()
            .Include(x => x.OrganizationClient)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.PublicToken == token);

        if (estimate == null)
            return Result.Failure<EstimateDto>(EstimateErrors.InvalidPublicLink);

        if (estimate.PublicTokenExpiresAt.HasValue && estimate.PublicTokenExpiresAt.Value < DateTimeOffset.UtcNow)
            return Result.Failure<EstimateDto>(EstimateErrors.PublicLinkExpired);

        return Result<EstimateDto>.Success(ToDto(estimate));
    }

    public async Task<Result<byte[]>> GetPublicPdfAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<byte[]>(EstimateErrors.InvalidPublicLink);

        var estimate = await estimates.Query()
            .Include(x => x.OrganizationClient)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.PublicToken == token);

        if (estimate == null)
            return Result.Failure<byte[]>(EstimateErrors.InvalidPublicLink);

        if (estimate.PublicTokenExpiresAt.HasValue && estimate.PublicTokenExpiresAt.Value < DateTimeOffset.UtcNow)
            return Result.Failure<byte[]>(EstimateErrors.PublicLinkExpired);

        var pdf = await pdfGenerator.GenerateEstimatePdfAsync(estimate);
        return Result<byte[]>.Success(pdf);
    }

    public Task<Result<EstimateDto>> AcceptAsync(Guid id, Guid organizationId, Guid organizationClientId)
    {
        return RespondAsync(id, organizationId, organizationClientId, EstimateStatus.Accepted);
    }

    public Task<Result<EstimateDto>> DeclineAsync(Guid id, Guid organizationId, Guid organizationClientId)
    {
        return RespondAsync(id, organizationId, organizationClientId, EstimateStatus.Declined);
    }

    private async Task<Result<EstimateDto>> RespondAsync(
        Guid id,
        Guid organizationId,
        Guid organizationClientId,
        EstimateStatus newStatus)
    {
        var estimate = await estimates.Query()
            .Include(x => x.OrganizationClient)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (estimate == null)
            return Result.Failure<EstimateDto>(EstimateErrors.NotFound);

        if (estimate.OrganizationId != organizationId || estimate.OrganizationClientId != organizationClientId)
            return Result.Failure<EstimateDto>(EstimateErrors.NotFound);

        if (estimate.Status != EstimateStatus.Sent)
            return Result.Failure<EstimateDto>(EstimateErrors.CannotRespondInCurrentStatus);

        estimate.Status = newStatus;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;

        estimates.Update(estimate);
        await unitOfWork.SaveChangesAsync();

        if (_followUpAutomation != null && newStatus is EstimateStatus.Accepted or EstimateStatus.Declined)
        {
            var reason = newStatus == EstimateStatus.Accepted
                ? FollowUpStopReason.EstimateAccepted
                : FollowUpStopReason.EstimateDeclined;

            await _followUpAutomation.StopEstimateSequenceAsync(estimate.Id, reason);
        }

        if (newStatus == EstimateStatus.Accepted)
        {
            await CreateJobFromEstimateAsync(estimate);
        }

        return Result<EstimateDto>.Success(ToDto(estimate));
    }

    private static void RecalculateTotals(Estimate estimate)
    {
        estimate.Subtotal = Math.Round(estimate.LineItems.Sum(x => x.Total), 2);
        estimate.TaxTotal = 0m;
        estimate.Total = Math.Round(estimate.Subtotal + estimate.TaxTotal, 2);
    }

    private async Task CreateJobFromEstimateAsync(Estimate estimate)
    {
        var job = new Job
        {
            OrganizationClientId = estimate.OrganizationClientId,
            EstimateId = estimate.Id,
            Title = estimate.Title ?? $"Job from {estimate.EstimateNumber}",
            Comments = estimate.Description,
            LifecycleStatus = JobLifecycleStatus.Approved
        };

        await _jobService.UpsertJobAsync(job, estimate.OrganizationId);
    }

    private async Task<string> GenerateEstimateNumberAsync(Guid organizationId)
    {
        var prefix = $"EST-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await estimates.Query()
            .CountAsync(x => x.OrganizationId == organizationId && x.EstimateNumber.StartsWith(prefix));
        return $"{prefix}{count + 1:0000}";
    }

    private static string GeneratePublicToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static EstimateDto ToDto(Estimate e) =>
        new(
            e.Id,
            e.OrganizationId,
            e.OrganizationClientId,
            e.EstimateNumber,
            e.Status,
            e.Title,
            e.Description,
            e.Notes,
            e.Subtotal,
            e.TaxTotal,
            e.Total,
            e.CreatedAt,
            e.UpdatedAt,
            e.SentAt,
            e.PublicToken,
            ToOrganizationClientDto(e),
            e.LineItems.Select(li => new EstimateLineItemDto(li.Id, li.Name, li.Description, li.Quantity, li.UnitPrice, li.Total)).ToList()
        );

    private static EstimateDto ToDtoWithoutLineItems(Estimate e) =>
        new(
            e.Id,
            e.OrganizationId,
            e.OrganizationClientId,
            e.EstimateNumber,
            e.Status,
            e.Title,
            e.Description,
            e.Notes,
            e.Subtotal,
            e.TaxTotal,
            e.Total,
            e.CreatedAt,
            e.UpdatedAt,
            e.SentAt,
            e.PublicToken,
            ToOrganizationClientDto(e),
            Array.Empty<EstimateLineItemDto>()
        );

    private static OrganizationClientDto ToOrganizationClientDto(Estimate e)
    {
        if (e.OrganizationClient != null)
        {
            var c = e.OrganizationClient;

            return new OrganizationClientDto
            {
                Id = c.Id,
                OrganizationId = c.OrganizationId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Address1 = c.Address1,
                Address2 = c.Address2,
                City = c.City,
                State = c.State,
                ZipCode = c.ZipCode,
                PhoneNumber = c.PhoneNumber,
                EmailAddress = c.EmailAddress,
                Organization = c.Organization == null
                    ? null
                    : new OrganizationDto
                    {
                        Id = c.Organization.Id,
                        OrganizationName = c.Organization.OrganizationName,
                        Email = c.Organization.EmailAddress,
                        Address1 = c.Organization.Address1,
                        Address2 = c.Organization.Address2,
                        City = c.Organization.City,
                        State = c.Organization.State,
                        ZipCode = c.Organization.ZipCode,
                        DefaultTaxRate = c.Organization.DefaultTaxRate,
                        PhoneNumber = c.Organization.PhoneNumber,
                        OnBoardingComplete = c.Organization.OnBoardingComplete,
                        CanAcceptPayments = c.Organization.CanAcceptPayments,
                        SubscriptionPlanName = c.Organization.SubscriptionPlanName
                    }
            };
        }

        return new OrganizationClientDto
        {
            Id = e.OrganizationClientId,
            OrganizationId = e.OrganizationId
        };
    }
}