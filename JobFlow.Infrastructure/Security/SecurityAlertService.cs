using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Security;

[ScopedService]
public class SecurityAlertService : ISecurityAlertService
{
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SecurityAlertService> _logger;

    public SecurityAlertService(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<SecurityAlertService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task EvaluateRecentEventsAsync()
    {
        var now = DateTime.UtcNow;
        var tenMinutesAgo = now.AddMinutes(-10);

        var auditRepository = _unitOfWork.RepositoryOf<AuditLog>();
        var recent = await auditRepository
            .Query(x => x.CreatedAt >= tenMinutesAgo)
            .AsNoTracking()
            .ToListAsync();

        await DetectAuthFailuresAsync(recent, tenMinutesAgo, now);
        await DetectPaymentAbuseAsync(recent, tenMinutesAgo, now);
        await DetectWebhookFailuresAsync(recent, tenMinutesAgo, now);
    }

    private async Task DetectAuthFailuresAsync(List<AuditLog> logs, DateTime windowStart, DateTime windowEnd)
    {
        var groups = logs
            .Where(x => x.StatusCode == 401)
            .GroupBy(x => x.IpAddress ?? "unknown")
            .Where(g => g.Count() >= 10);

        foreach (var group in groups)
        {
            await RaiseAlertAsync(
                ruleKey: "AUTH_FAILED_SPIKE",
                category: "Authentication",
                severity: "High",
                description: $"Repeated authentication failures from IP '{group.Key}' in a short window.",
                evidenceCount: group.Count(),
                windowStart: windowStart,
                windowEnd: windowEnd,
                detailsJson: System.Text.Json.JsonSerializer.Serialize(new { ipAddress = group.Key }));
        }
    }

    private async Task DetectPaymentAbuseAsync(List<AuditLog> logs, DateTime windowStart, DateTime windowEnd)
    {
        var groups = logs
            .Where(x => x.Path != null
                        && x.Path.StartsWith("/api/payments", StringComparison.OrdinalIgnoreCase)
                        && (x.StatusCode == 403 || x.StatusCode == 429))
            .GroupBy(x => x.UserId?.ToString() ?? x.IpAddress ?? "unknown")
            .Where(g => g.Count() >= 5);

        foreach (var group in groups)
        {
            await RaiseAlertAsync(
                ruleKey: "PAYMENT_ABUSE_SPIKE",
                category: "Payment",
                severity: "High",
                description: $"Potential payment endpoint abuse detected for actor '{group.Key}'.",
                evidenceCount: group.Count(),
                windowStart: windowStart,
                windowEnd: windowEnd,
                detailsJson: System.Text.Json.JsonSerializer.Serialize(new { actor = group.Key }));
        }
    }

    private async Task DetectWebhookFailuresAsync(List<AuditLog> logs, DateTime windowStart, DateTime windowEnd)
    {
        var groups = logs
            .Where(x => x.Path != null
                        && (x.Path.Equals("/api/payments/webhook", StringComparison.OrdinalIgnoreCase)
                            || x.Path.Equals("/api/payments/square/webhook", StringComparison.OrdinalIgnoreCase))
                        && (x.StatusCode == 400 || x.StatusCode == 401))
            .GroupBy(x => x.IpAddress ?? "unknown")
            .Where(g => g.Count() >= 3);

        foreach (var group in groups)
        {
            await RaiseAlertAsync(
                ruleKey: "WEBHOOK_FAILURE_SPIKE",
                category: "Webhook",
                severity: "Medium",
                description: $"Repeated webhook validation failures from IP '{group.Key}'.",
                evidenceCount: group.Count(),
                windowStart: windowStart,
                windowEnd: windowEnd,
                detailsJson: System.Text.Json.JsonSerializer.Serialize(new { ipAddress = group.Key }));
        }
    }

    private async Task RaiseAlertAsync(
        string ruleKey,
        string category,
        string severity,
        string description,
        int evidenceCount,
        DateTime windowStart,
        DateTime windowEnd,
        string? detailsJson)
    {
        var alertRepo = _unitOfWork.RepositoryOf<SecurityAlert>();
        var exists = await alertRepo.ExistsAsync(x =>
            x.RuleKey == ruleKey
            && x.Status == "Open"
            && x.CreatedAt >= DateTime.UtcNow.AddMinutes(-30));

        if (exists)
            return;

        var alert = new SecurityAlert
        {
            RuleKey = ruleKey,
            Category = category,
            Severity = severity,
            Description = description,
            EvidenceCount = evidenceCount,
            WindowStartUtc = windowStart,
            WindowEndUtc = windowEnd,
            Status = "Open",
            DetailsJson = detailsJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        await alertRepo.AddAsync(alert);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning(
            "Security alert raised: {RuleKey} - {Description} (Count: {Count})",
            ruleKey,
            description,
            evidenceCount);

        await _auditLogService.WriteAsync(new Business.Models.DTOs.AuditLogWriteRequest
        {
            Category = "SecurityAlert",
            Action = "Raised",
            ResourceType = nameof(SecurityAlert),
            ResourceId = alert.Id.ToString(),
            StatusCode = 200,
            Success = true,
            Path = "system://security-alerts",
            Method = "SYSTEM",
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                alert.RuleKey,
                alert.Category,
                alert.Severity,
                alert.Description,
                alert.EvidenceCount
            })
        });
    }
}
