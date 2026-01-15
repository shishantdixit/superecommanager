using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Ndr;

/// <summary>
/// Lightweight DTO for NDR case list view.
/// </summary>
public record NdrListDto
{
    public Guid Id { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string AwbNumber { get; init; } = string.Empty;
    public NdrStatus Status { get; init; }
    public NdrReasonCode ReasonCode { get; init; }
    public string? ReasonDescription { get; init; }
    public DateTime NdrDate { get; init; }
    public string? AssignedToUserName { get; init; }
    public int AttemptCount { get; init; }
    public DateTime? NextFollowUpAt { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string DeliveryCity { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Full DTO for NDR case details.
/// </summary>
public record NdrDetailDto
{
    public Guid Id { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ShipmentNumber { get; init; } = string.Empty;
    public string AwbNumber { get; init; } = string.Empty;
    public NdrStatus Status { get; init; }
    public NdrReasonCode ReasonCode { get; init; }
    public string? ReasonDescription { get; init; }
    public DateTime NdrDate { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public DateTime? AssignedAt { get; init; }
    public int AttemptCount { get; init; }
    public DateTime? NextFollowUpAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? Resolution { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Customer info
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }

    // Delivery address
    public AddressDto DeliveryAddress { get; init; } = null!;

    // COD info
    public bool IsCOD { get; init; }
    public decimal? CODAmount { get; init; }

    // Actions and remarks
    public List<NdrActionDto> Actions { get; init; } = new();
    public List<NdrRemarkDto> Remarks { get; init; } = new();
}

/// <summary>
/// DTO for NDR action.
/// </summary>
public record NdrActionDto
{
    public Guid Id { get; init; }
    public NdrActionType ActionType { get; init; }
    public Guid PerformedByUserId { get; init; }
    public string PerformedByUserName { get; init; } = string.Empty;
    public DateTime PerformedAt { get; init; }
    public string? Details { get; init; }
    public string? Outcome { get; init; }
    public int? CallDurationSeconds { get; init; }
}

/// <summary>
/// DTO for NDR remark.
/// </summary>
public record NdrRemarkDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsInternal { get; init; }
}

/// <summary>
/// Address DTO for NDR (reusing from Shipments).
/// </summary>
public record AddressDto
{
    public string Name { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "India";
    public string? Phone { get; init; }
}

/// <summary>
/// Filter parameters for NDR cases query.
/// </summary>
public record NdrFilterDto
{
    public string? SearchTerm { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? ShipmentId { get; init; }
    public NdrStatus? Status { get; init; }
    public List<NdrStatus>? Statuses { get; init; }
    public NdrReasonCode? ReasonCode { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public bool? Unassigned { get; init; }
    public bool? HasFollowUpDue { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// Sort options for NDR cases.
/// </summary>
public enum NdrSortBy
{
    NdrDate,
    CreatedAt,
    NextFollowUpAt,
    Status,
    AttemptCount
}

/// <summary>
/// NDR statistics DTO.
/// </summary>
public record NdrStatsDto
{
    public int TotalOpen { get; init; }
    public int TotalAssigned { get; init; }
    public int TotalPendingFollowUp { get; init; }
    public int TotalReattemptScheduled { get; init; }
    public int TotalDelivered { get; init; }
    public int TotalRTO { get; init; }
    public int TotalClosedToday { get; init; }
    public int TotalOpenedToday { get; init; }

    public Dictionary<NdrReasonCode, int> ByReasonCode { get; init; } = new();
    public Dictionary<string, int> ByAssignee { get; init; } = new();
    public decimal DeliverySuccessRate { get; init; }
    public decimal AverageResolutionHours { get; init; }
}

/// <summary>
/// DTO for call log entry.
/// </summary>
public record CallLogDto
{
    public NdrActionType ActionType { get; init; }
    public string? Details { get; init; }
    public string? Outcome { get; init; }
    public int? CallDurationSeconds { get; init; }
}

/// <summary>
/// DTO for scheduling reattempt.
/// </summary>
public record ReattemptScheduleDto
{
    public DateTime ReattemptDate { get; init; }
    public string? UpdatedAddress { get; init; }
    public string? Remarks { get; init; }
}
