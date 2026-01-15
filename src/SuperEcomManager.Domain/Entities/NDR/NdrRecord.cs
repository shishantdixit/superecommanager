using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.NDR;

/// <summary>
/// Represents a Non-Delivery Report (NDR) case.
/// </summary>
public class NdrRecord : AuditableEntity
{
    public Guid ShipmentId { get; private set; }
    public Guid OrderId { get; private set; }
    public string AwbNumber { get; private set; } = string.Empty;
    public NdrStatus Status { get; private set; }
    public NdrReasonCode ReasonCode { get; private set; }
    public string? ReasonDescription { get; private set; }
    public DateTime NdrDate { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? NextFollowUpAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? Resolution { get; private set; }

    private readonly List<NdrAction> _actions = new();
    public IReadOnlyCollection<NdrAction> Actions => _actions.AsReadOnly();

    private readonly List<NdrRemark> _remarks = new();
    public IReadOnlyCollection<NdrRemark> Remarks => _remarks.AsReadOnly();

    private NdrRecord() { }

    public static NdrRecord Create(
        Guid shipmentId,
        Guid orderId,
        string awbNumber,
        NdrReasonCode reasonCode,
        string? reasonDescription = null)
    {
        return new NdrRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            OrderId = orderId,
            AwbNumber = awbNumber,
            Status = NdrStatus.Open,
            ReasonCode = reasonCode,
            ReasonDescription = reasonDescription,
            NdrDate = DateTime.UtcNow,
            AttemptCount = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignTo(Guid userId)
    {
        AssignedToUserId = userId;
        AssignedAt = DateTime.UtcNow;
        Status = NdrStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAction(NdrAction action)
    {
        _actions.Add(action);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRemark(NdrRemark remark)
    {
        _remarks.Add(remark);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ScheduleReattempt(DateTime reattemptDate)
    {
        Status = NdrStatus.ReattemptScheduled;
        NextFollowUpAt = reattemptDate;
        AttemptCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve(NdrStatus resolution, string? notes = null)
    {
        Status = resolution;
        Resolution = notes;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
