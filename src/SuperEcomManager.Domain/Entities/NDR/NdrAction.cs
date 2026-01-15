using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.NDR;

/// <summary>
/// Represents an action taken for an NDR case.
/// </summary>
public class NdrAction : BaseEntity
{
    public Guid NdrRecordId { get; private set; }
    public NdrActionType ActionType { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public DateTime PerformedAt { get; private set; }
    public string? Details { get; private set; }
    public string? Outcome { get; private set; }
    public int? CallDurationSeconds { get; private set; }

    public NdrRecord? NdrRecord { get; private set; }

    private NdrAction() { }

    public static NdrAction Create(
        Guid ndrRecordId,
        NdrActionType actionType,
        Guid performedByUserId,
        string? details = null,
        string? outcome = null,
        int? callDurationSeconds = null)
    {
        return new NdrAction
        {
            Id = Guid.NewGuid(),
            NdrRecordId = ndrRecordId,
            ActionType = actionType,
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            Details = details,
            Outcome = outcome,
            CallDurationSeconds = callDurationSeconds
        };
    }
}
