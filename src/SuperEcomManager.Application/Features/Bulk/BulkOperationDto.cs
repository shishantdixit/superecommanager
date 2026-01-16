using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Bulk;

/// <summary>
/// Result of a bulk operation.
/// </summary>
public record BulkOperationResultDto
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<BulkOperationErrorDto> Errors { get; init; } = new();
    public List<Guid> SuccessfulIds { get; init; } = new();
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Error details for a single item in bulk operation.
/// </summary>
public record BulkOperationErrorDto
{
    public int Index { get; init; }
    public Guid? ItemId { get; init; }
    public string? Reference { get; init; }
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// Request to update multiple orders.
/// </summary>
public record BulkOrderUpdateRequestDto
{
    public List<Guid> OrderIds { get; init; } = new();
    public OrderStatus? NewStatus { get; init; }
    public string? InternalNotes { get; init; }
    public bool? MarkAsPriority { get; init; }
}

/// <summary>
/// Request to create shipments for multiple orders.
/// </summary>
public record BulkShipmentCreateRequestDto
{
    public List<Guid> OrderIds { get; init; } = new();
    public CourierType CourierType { get; init; }
    public Guid? CourierAccountId { get; init; }
}

/// <summary>
/// Request to assign NDR cases to an agent.
/// </summary>
public record BulkNdrAssignRequestDto
{
    public List<Guid> NdrIds { get; init; } = new();
    public Guid AssignToUserId { get; init; }
}

/// <summary>
/// Request to update NDR status in bulk.
/// </summary>
public record BulkNdrStatusUpdateRequestDto
{
    public List<Guid> NdrIds { get; init; } = new();
    public NdrStatus NewStatus { get; init; }
    public string? Remarks { get; init; }
}

/// <summary>
/// CSV import result.
/// </summary>
public record CsvImportResultDto
{
    public int TotalRows { get; init; }
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public List<CsvImportErrorDto> Errors { get; init; } = new();
    public List<Guid> ImportedIds { get; init; } = new();
}

/// <summary>
/// CSV import error details.
/// </summary>
public record CsvImportErrorDto
{
    public int RowNumber { get; init; }
    public string? ColumnName { get; init; }
    public string? Value { get; init; }
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// CSV export request.
/// </summary>
public record CsvExportRequestDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public List<string>? Columns { get; init; }
    public OrderStatus? StatusFilter { get; init; }
    public Guid? ChannelFilter { get; init; }
}

/// <summary>
/// CSV export result.
/// </summary>
public record CsvExportResultDto
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = "text/csv";
    public int RowCount { get; init; }
}

/// <summary>
/// Order row for CSV import.
/// </summary>
public record OrderImportRowDto
{
    public string? ExternalOrderId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingAddress { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingState { get; init; }
    public string? ShippingPostalCode { get; init; }
    public string? ShippingCountry { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? PaymentMethod { get; init; }
    public string? ProductSku { get; init; }
    public string? ProductName { get; init; }
    public int? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
}

/// <summary>
/// Product row for CSV import.
/// </summary>
public record ProductImportRowDto
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public decimal? CostPrice { get; init; }
    public int? StockQuantity { get; init; }
    public int? ReorderLevel { get; init; }
    public string? Category { get; init; }
    public decimal? Weight { get; init; }
}
