using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public sealed record EstimateLineItemDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total
);

public sealed record EstimateDto(
    Guid Id,
    Guid OrganizationId,
    Guid OrganizationClientId,
    string EstimateNumber,
    EstimateStatus Status,
    string? Title,
    string? Description,
    string? Notes,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? SentAt,
    string PublicToken,
    OrganizationClientDto OrganizationClient,
    IReadOnlyList<EstimateLineItemDto> LineItems
);

public sealed record CreateEstimateLineItemRequest(
    string Name,
    string? Description,
    decimal Quantity,
    decimal UnitPrice
);

public sealed record CreateEstimateRequest(
    Guid OrganizationId,
    Guid OrganizationClientId,
    string? Title,
    string? Description,
    string? Notes,
    IReadOnlyList<CreateEstimateLineItemRequest> LineItems
);

public sealed record UpdateEstimateRequest(
    string? Title,
    string? Description,
    string? Notes,
    IReadOnlyList<CreateEstimateLineItemRequest> LineItems
);

public sealed record SendEstimateRequest(
    bool SendEmail,
    bool SendSms
);