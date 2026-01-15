using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.NDR;

/// <summary>
/// Represents a remark/note for an NDR case.
/// </summary>
public class NdrRemark : BaseEntity
{
    public Guid NdrRecordId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsInternal { get; private set; }

    public NdrRecord? NdrRecord { get; private set; }

    private NdrRemark() { }

    public NdrRemark(Guid ndrRecordId, Guid userId, string content, bool isInternal = true)
    {
        Id = Guid.NewGuid();
        NdrRecordId = ndrRecordId;
        UserId = userId;
        Content = content;
        IsInternal = isInternal;
        CreatedAt = DateTime.UtcNow;
    }
}
